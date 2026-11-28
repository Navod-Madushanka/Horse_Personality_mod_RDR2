using System;
using System.Collections.Generic;
using System.Globalization;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>What the game layer saw this check, for the coat signature habits (Phase 7b).</summary>
    public struct SignatureSituation
    {
        public bool Mounted;
        public bool PlayerNear;
        public double PlayerDistance;
        public double SpeedMs;
        public double StandingStillSeconds;
        public int GameHour;
        public int BondLevel;
        /// <summary>_GET_MAP_ZONE_AT_COORDS type TOWN is not 0 at the horse.</summary>
        public bool InTown;
        /// <summary>A person within Town disdain range, in town (0 for none).</summary>
        public int TownPerson;
        /// <summary>The player started aiming this check (was not aiming the check before).</summary>
        public bool StartedAiming;
        /// <summary>Humans within Prankster enemy range in combat with the player.</summary>
        public int EnemiesInCombat;
        /// <summary>The player has fired since the enemies showed up (Prankster waits for the quiet before the shooting).</summary>
        public bool PlayerFiredRecently;
        /// <summary>Upward speed (m/s) from GET_ENTITY_VELOCITY.</summary>
        public double ClimbSpeed;
        public int DirtRank;
        /// <summary>A human stranger within Soul stare range (0 for none).</summary>
        public int StrangerClose;
        /// <summary>A human NPC within Camp creeper range (0 for none).</summary>
        public int CreepTarget;
        /// <summary>How long the player has aimed without a break (0 when not aiming).</summary>
        public double AimingSeconds;
        /// <summary>Another horse within Short king range (0 for none).</summary>
        public int OtherHorse;
        /// <summary>A hogtied person within Bounty stomper range (0 for none).</summary>
        public int HogtiedPerson;
        /// <summary>A stranger within Guard range of the horse (0 for none).</summary>
        public int GuardTarget;
        /// <summary>Seconds ridden at a walk without a break.</summary>
        public double WalkingSeconds;
        /// <summary>An enemy fighting you within War horse range (0 for none).</summary>
        public int NearEnemy;
        /// <summary>A cheaper horse within Short king range: not a higher stat tier, not its own breed (0 for none).</summary>
        public int LowerTierHorse;
        /// <summary>It came to a stop from a gallop in the last few seconds.</summary>
        public bool JustStoppedFromGallop;
        /// <summary>People within Showboat range.</summary>
        public int Crowd;
        /// <summary>A moving horse ahead within Must lead range (0 for none).</summary>
        public int HorseAhead;
        /// <summary>Another moving horse within Racer range (0 for none).</summary>
        public int Challenger;
        /// <summary>Seconds since it was last at a gallop.</summary>
        public double SecondsSinceGallop;
        /// <summary>How long its last gallop lasted.</summary>
        public double LastGallopSeconds;
        /// <summary>Someone hurrying toward the player within Stranger danger range (0 for none).</summary>
        public int RushingPerson;
        /// <summary>Seconds since the last danger (threat, gunfire, fight); large when there was none.</summary>
        public double SecondsSinceDanger;
        /// <summary>You patted or calmed it since that danger.</summary>
        public bool SoothedSinceDanger;
        /// <summary>IS_ENTITY_IN_WATER on the horse.</summary>
        public bool InWater;
        /// <summary>A predator first seen this check within Sixth sense range (0 for none).</summary>
        public int SensedPredator;
        /// <summary>A predator within Curious range (0 for none).</summary>
        public int CuriousTarget;
        /// <summary>Someone within Crowd shy range (0 for none).</summary>
        public int CrowdPerson;
        /// <summary>Sprint presses in the last Spur spam seconds.</summary>
        public int RecentSpurPresses;
        /// <summary>Minutes ridden since its last treat (Treat tantrum).</summary>
        public double MinutesSinceTreat;
    }

    /// <summary>
    /// Coat signature habits (Build plan: Phase 7b; the sheet's Coats and Signatures tabs). Only for horses whose roll
    /// carries the signature. Pure, like BehaviorBrain: the runner builds a <see cref="SignatureSituation"/>, plays the
    /// answer and logs the cause. Held in missions and cutscenes with everything else, so Roadside grazer never grazes
    /// mid-mission (hard rule). Rates are per minute, turned into a chance per check.
    ///
    /// Lazy pace is not here: it is a speed cap the runner applies while you aren't spurring (<see cref="LazyCap"/>).
    /// Snake drama, Night owl, City lover and Wolf brawler change the threat and spook rules (runner scan, BehaviorBrain).
    /// Brush excitement is a care reaction (CareReactor.BrushStarted).
    /// </summary>
    public sealed class SignatureBrain
    {
        private readonly BehaviorTuning _t;
        private readonly Func<double> _random;
        private readonly HashSet<int> _grazedThisStop = new HashSet<int>();
        private readonly HashSet<int> _escapedThisStop = new HashSet<int>();
        private readonly HashSet<int> _roamedThisStop = new HashSet<int>();
        private readonly HashSet<int> _boredThisAim = new HashSet<int>();
        private readonly HashSet<int> _stopHandled = new HashSet<int>();

        public SignatureBrain(BehaviorTuning tuning, Func<double> random)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Lazy pace: the move blend ratio to cap it at while you aren't spurring, or null for no cap.</summary>
        /// <param name="bribed">Bribable: a loved treat in the last Bribe seconds lifts the cap (Belgian Mealy Chestnut, Nov 28).</param>
        public double? LazyCap(Personality p, bool mounted, bool spurring, bool bribed = false) =>
            p != null && p.Has(Signature.LazyPace) && mounted && !spurring && !(bribed && p.Has(Signature.Bribable)) ? _t.LazyWalkRatio : (double?)null;

        public Decision Decide(int horse, Personality p, SignatureSituation s, double dt, CooldownBook cooldowns, double now)
        {
            if (p == null || p.Signatures.Count == 0) return Decision.None;
            bool far = !s.Mounted && !s.PlayerNear;
            if (far && !WatchedFar(p)) return Decision.None;
            if (s.StandingStillSeconds <= 0) { _grazedThisStop.Remove(horse); _escapedThisStop.Remove(horse); _roamedThisStop.Remove(horse); }
            if (s.AimingSeconds <= 0) _boredThisAim.Remove(horse);

            // Answers to a moment come first: they are once per moment, not a rate.
            if (p.Has(Signature.DramaticRear) && s.Mounted && s.StartedAiming && s.StandingStillSeconds > 0 && IsDusk(s.GameHour)
                && Ready(horse, cooldowns, now))
            {
                double roll = _random();
                if (roll < _t.DramaticRearChance)
                    return Use(horse, BehaviorAction.Rear, $"Dramatic rear: you drew at {s.GameHour}:00, standing still: roll {F3(roll)} vs {F3(_t.DramaticRearChance)}", cooldowns, now);
            }
            if (p.Has(Signature.EscapeArtist) && !s.Mounted && s.PlayerDistance >= _t.EscapePlayerDistance
                && s.StandingStillSeconds >= _t.EscapeAfterSeconds && !_escapedThisStop.Contains(horse) && Ready(horse, cooldowns, now))
            {
                _escapedThisStop.Add(horse);
                return Use(horse, BehaviorAction.Unhitch, $"Escape artist: left standing {s.StandingStillSeconds:0} s with you {s.PlayerDistance:0} m away", cooldowns, now);
            }
            if (p.Has(Signature.Wanderlust) && !s.Mounted && s.PlayerDistance >= _t.WanderlustPlayerDistance
                && s.StandingStillSeconds >= _t.WanderlustDelaySeconds && !_roamedThisStop.Contains(horse) && Ready(horse, cooldowns, now))
            {
                _roamedThisStop.Add(horse);
                return Use(horse, BehaviorAction.Roam, $"Wanderlust: untied {s.StandingStillSeconds:0} s with you {s.PlayerDistance:0} m away, off to see the next hill", cooldowns, now);
            }
            if (p.Has(Signature.GuardDog) && !s.Mounted && s.GuardTarget != 0 && Ready(horse, cooldowns, now) && dt > 0)
            {
                double roll = _random(), chance = 1 - Math.Exp(-_t.GuardRate * dt / 60.0);
                if (roll < chance)
                {
                    Decision g = Use(horse, BehaviorAction.SquareUp, $"Guard dog: a stranger at your saddlebags: {F3(_t.GuardRate)}/min", cooldowns, now);
                    g.Target = s.GuardTarget;
                    return g;
                }
            }
            if (p.Has(Signature.SeparationAnxiety) && !s.Mounted && s.StandingStillSeconds > 0 && s.PlayerDistance >= _t.SeparationDistance
                && Ready(horse, cooldowns, now) && dt > 0 && _random() < 1 - Math.Exp(-_t.SeparationRate * dt / 60.0))
                return Use(horse, BehaviorAction.Call, $"Separation anxiety: you're {s.PlayerDistance:0} m away and it thinks you left forever", cooldowns, now);
            if (far) return Decision.None; // only the habits above work with you far away

            // A stop after a gallop: No brakes (a hard stop), Spoiled (a long gallop). One answer per stop.
            if (s.SecondsSinceGallop <= 0) _stopHandled.Remove(horse);
            else if (s.Mounted && s.SpeedMs < _t.WalkSpeed && !_stopHandled.Contains(horse) && Ready(horse, cooldowns, now))
            {
                if (p.Has(Signature.NoBrakes) && s.SecondsSinceGallop <= _t.NoBrakesSeconds)
                {
                    _stopHandled.Add(horse);
                    double roll = _random();
                    if (roll < _t.NoBrakesChance)
                        return Use(horse, BehaviorAction.Skid, $"No brakes: a hard stop from a gallop: roll {F3(roll)} vs {F3(_t.NoBrakesChance)}", cooldowns, now);
                }
                if (p.Has(Signature.Spoiled) && s.LastGallopSeconds >= _t.SpoiledGallopSeconds && s.StandingStillSeconds > 0)
                {
                    _stopHandled.Add(horse);
                    double roll = _random();
                    if (roll < _t.SpoiledChance)
                        return Use(horse, BehaviorAction.Call, $"Spoiled: {s.LastGallopSeconds:0} s of galloping deserves a sugar cube: roll {F3(roll)} vs {F3(_t.SpoiledChance)}", cooldowns, now);
                }
            }
            if (p.Has(Signature.BoredBuck) && s.Mounted && s.StandingStillSeconds > 0 && s.AimingSeconds >= _t.BoredAimSeconds
                && !_boredThisAim.Contains(horse) && Ready(horse, cooldowns, now))
            {
                _boredThisAim.Add(horse);
                double roll = _random();
                if (roll < _t.BoredBuckChance)
                    return Use(horse, s.BondLevel <= 2 ? BehaviorAction.ThrowRider : BehaviorAction.Agitate,
                        $"Bored buck: you aimed {s.AimingSeconds:0} s, bond {s.BondLevel}: roll {F3(roll)} vs {F3(_t.BoredBuckChance)}", cooldowns, now);
            }
            if (p.Has(Signature.Showboat) && s.Mounted && s.JustStoppedFromGallop && s.Crowd >= _t.ShowboatCrowd && Ready(horse, cooldowns, now))
            {
                double roll = _random();
                Decision show = Use(horse, roll < _t.ShowboatChance ? BehaviorAction.Rear : BehaviorAction.None,
                    $"Showboat: a big stop in front of {s.Crowd} people: roll {F3(roll)} vs {F3(_t.ShowboatChance)}", cooldowns, now);
                if (!show.IsNone) return show;
            }
            if (p.Has(Signature.WarHorse) && !s.Mounted && s.NearEnemy != 0 && Ready(horse, cooldowns, now))
            {
                Decision war = Use(horse, BehaviorAction.Brawl, "War horse: goes for the enemy at your side", cooldowns, now);
                war.Target = s.NearEnemy;
                return war;
            }
            if (p.Has(Signature.SixthSense) && s.SensedPredator != 0 && (s.Mounted || s.PlayerNear))
            {
                // An answer to a moment (one per predator, the runner's scan dedups), so it ignores the shared cooldown.
                return new Decision { Action = BehaviorAction.Snub, Group = BehaviorGroup.Tell, Target = s.SensedPredator,
                    Cause = "Sixth sense: something out there, before you've seen it" };
            }
            if (p.Has(Signature.SpurSnob) && s.Mounted && s.RecentSpurPresses >= _t.SpurSpamPresses && Ready(horse, cooldowns, now))
                return Use(horse, BehaviorAction.Snub, $"Spur snob: {s.RecentSpurPresses} spur presses in {_t.SpurSpamSeconds:0.#} s is no way to treat a Warmblood", cooldowns, now);
            if (p.Has(Signature.RoadsideGrazer) && s.Mounted && s.StandingStillSeconds >= _t.GrazerIdleSeconds
                && !_grazedThisStop.Contains(horse) && Ready(horse, cooldowns, now))
            {
                _grazedThisStop.Add(horse);
                return Use(horse, BehaviorAction.Graze, $"Roadside grazer: stood {s.StandingStillSeconds:0} s with you on it", cooldowns, now);
            }
            if (!Ready(horse, cooldowns, now) || dt <= 0) return Decision.None;

            bool walking = s.Mounted && s.SpeedMs >= _t.StandingStillSpeed && s.SpeedMs < _t.WalkSpeed;
            bool galloping = s.Mounted && s.SpeedMs >= _t.GallopSpeed;
            var candidates = new List<(BehaviorAction Action, double PerMinute, string Why, int Target)>();
            if (p.Has(Signature.MoodyRefusal) && walking)
                candidates.Add((BehaviorAction.QuickStop, _t.MoodyRefusalRate * (s.BondLevel <= 1 ? 1.5 : 1), "Moody refusal: in a mood at a walk" + (s.BondLevel <= 1 ? ", weak bond" : ""), 0));
            if (p.Has(Signature.SnackMagnet) && walking)
                candidates.Add((BehaviorAction.SnackStop, _t.SnackMagnetRate, "Snack magnet: spotted something tasty", 0));
            if (p.Has(Signature.Clumsy) && s.Mounted && s.SpeedMs >= _t.WalkSpeed && s.SpeedMs < _t.GallopSpeed)
                candidates.Add((BehaviorAction.Skid, _t.ClumsyRate, "Clumsy: tripped over its own hooves", 0));
            if (p.Has(Signature.Restless) && (s.Mounted || s.PlayerNear) && s.StandingStillSeconds >= _t.RestlessSeconds)
                candidates.Add((BehaviorAction.Agitate, _t.RestlessRate, $"Restless: kept standing {s.StandingStillSeconds:0} s", 0));
            if (p.Has(Signature.TrailSnob) && walking && s.WalkingSeconds >= _t.TrailSnobSeconds)
                candidates.Add((BehaviorAction.Snub, _t.TrailSnobRate, $"Trail snob: {s.WalkingSeconds:0} s of walking is beneath it", 0));
            if (p.Has(Signature.Snob) && s.LowerTierHorse != 0)
                candidates.Add((BehaviorAction.Snub, _t.SnobRate, "Snob: ears back at a cheaper horse", s.LowerTierHorse));
            if (p.Has(Signature.MustLead) && s.Mounted && s.SpeedMs >= _t.WalkSpeed && s.HorseAhead != 0)
                candidates.Add((BehaviorAction.Agitate, _t.MustLeadRate, "Must lead: stuck behind another horse", 0));
            if (p.Has(Signature.NeedsSoothing) && (s.Mounted || s.PlayerNear) && !s.SoothedSinceDanger
                && s.SecondsSinceDanger >= _t.NeedsSoothingDelay && s.SecondsSinceDanger <= _t.NeedsSoothingSeconds)
                candidates.Add((BehaviorAction.Fret, _t.NeedsSoothingRate, $"Needs soothing: still rattled {s.SecondsSinceDanger:0} s after the danger", 0));
            if (p.Has(Signature.NightStare) && !s.Mounted && s.PlayerNear && s.StandingStillSeconds > 0 && IsNight(s.GameHour))
                candidates.Add((BehaviorAction.StareAtNothing, _t.NightStareRate, "Night stare: staring at an empty corner", 0));
            if (p.Has(Signature.Curious) && !s.Mounted && s.PlayerNear && s.CuriousTarget != 0)
                candidates.Add((BehaviorAction.Creep, _t.CuriousRate, "Curious: wants a closer look at the predator", s.CuriousTarget));
            if (p.Has(Signature.CrowdShy) && !s.Mounted && s.CrowdPerson != 0)
                candidates.Add((BehaviorAction.SideStep, _t.CrowdShyRate, "Crowd shy: side-steps away from a stranger", s.CrowdPerson));
            if (p.Has(Signature.CountryBumpkin) && s.Mounted && s.InTown)
                candidates.Add((BehaviorAction.QuickStop, _t.BumpkinRate, "Country bumpkin: overwhelmed by the big town", 0));
            if (p.Has(Signature.Balker) && s.Mounted && s.SpeedMs >= _t.StandingStillSpeed && (s.ClimbSpeed >= _t.HillClimbSpeed || s.InWater))
                candidates.Add((BehaviorAction.QuickStop, _t.BalkRate, s.InWater ? "Balker: that river is too deep" : "Balker: that trail is too steep", 0));
            if (p.Has(Signature.WaterSigh) && s.Mounted && s.InWater && s.SpeedMs >= _t.StandingStillSpeed)
                candidates.Add((BehaviorAction.Sigh, _t.WaterSighRate, "Water sigh: wet hooves, again", 0));
            if (p.Has(Signature.AppleBeggar) && !s.Mounted && s.PlayerNear && s.PlayerDistance <= _t.BeggarRange && s.StandingStillSeconds >= _t.BeggarSeconds)
                candidates.Add((BehaviorAction.Beg, _t.BeggarRate, "Apple beggar: you've been standing here long enough to hand over an apple", 0));
            if (p.Has(Signature.TreatTantrum) && s.Mounted && s.MinutesSinceTreat >= _t.TantrumAfterMinutes)
                candidates.Add((BehaviorAction.Agitate, _t.TantrumRate, $"Treat tantrum: {s.MinutesSinceTreat:0} minutes and not one oatcake", 0));
            if (p.Has(Signature.CliffNerves) && galloping && -s.ClimbSpeed >= _t.DownhillSpeed)
                candidates.Add((BehaviorAction.Agitate, _t.CliffNervesRate, $"Cliff nerves: galloping down a steep slope ({-s.ClimbSpeed:0.0} m/s)", 0));
            if (p.Has(Signature.DownhillSnob) && walking && -s.ClimbSpeed >= _t.DownhillSpeed)
                candidates.Add((BehaviorAction.Snub, _t.DownhillSnobRate, $"Downhill snob: held to a walk going down {-s.ClimbSpeed:0.0} m/s", 0));
            if (p.Has(Signature.StrangerDanger) && s.RushingPerson != 0)
                candidates.Add((BehaviorAction.SquareUp, _t.StrangerDangerRate, "Stranger danger: someone's rushing you", s.RushingPerson));
            if (p.Has(Signature.Racer) && s.Mounted && s.SpeedMs >= _t.WalkSpeed && s.Challenger != 0)
                candidates.Add((BehaviorAction.Call, _t.RacerRate, "Racer: a challenger alongside", s.Challenger));
            if (p.Has(Signature.ShortKing) && s.OtherHorse != 0)
                candidates.Add((BehaviorAction.SquareUp, _t.ShortKingRate, "Short king: picking a fight with a bigger horse", s.OtherHorse));
            if (p.Has(Signature.BountyStomper) && s.HogtiedPerson != 0)
                candidates.Add((BehaviorAction.SquareUp, _t.BountyStomperRate, "Bounty stomper: stamping at the hogtied bounty", s.HogtiedPerson));
            if (p.Has(Signature.MudTantrum) && s.DirtRank >= _t.FussyDirtRank)
                candidates.Add((BehaviorAction.Agitate, _t.FussyRate, $"Mud tantrum: dirt rank {s.DirtRank}", 0));
            if (p.Has(Signature.TownDisdain) && s.InTown && s.TownPerson != 0)
                candidates.Add((BehaviorAction.Snub, _t.TownDisdainRate, "Town disdain: a peasant came too close", s.TownPerson));
            if (p.Has(Signature.Prankster) && s.Mounted && s.StandingStillSeconds > 0 && s.EnemiesInCombat > 0 && !s.PlayerFiredRecently)
                candidates.Add((BehaviorAction.ThrowRider, _t.PranksterRate, $"Prankster: {s.EnemiesInCombat} rival(s) squaring up, you haven't fired", 0));
            if (p.Has(Signature.HillSigh) && walking && s.ClimbSpeed >= _t.HillClimbSpeed)
                candidates.Add((BehaviorAction.Sigh, _t.HillSighRate, $"Hill sigh: climbing {s.ClimbSpeed:0.0} m/s", 0));
            if (p.Has(Signature.Scatterbrain) && galloping)
                candidates.Add((BehaviorAction.Skid, _t.ScatterbrainRate, "Scatterbrain: distracted at a gallop (not scared)", 0));
            if (p.Has(Signature.SoulStare) && s.StrangerClose != 0)
                candidates.Add((BehaviorAction.Stare, _t.SoulStareRate, "Soul stare: a stranger came close", s.StrangerClose));
            if (p.Has(Signature.CampCreeper) && !s.Mounted && s.PlayerNear && s.CreepTarget != 0)
                candidates.Add((BehaviorAction.Creep, _t.CreeperRate, "Camp creeper: someone to sneak up on", s.CreepTarget));

            foreach (var c in candidates)
            {
                double chance = 1 - Math.Exp(-c.PerMinute * dt / 60.0);
                double roll = _random();
                if (roll < chance)
                {
                    Decision d = Use(horse, c.Action, $"{c.Why}: {F3(c.PerMinute)}/min", cooldowns, now);
                    d.Target = c.Target;
                    return d;
                }
            }
            return Decision.None;
        }

        /// <summary>Habits that need the horse watched with you far away (the runner otherwise stops at 15 m).</summary>
        public static bool WatchedFar(Personality p) => p != null && (p.Has(Signature.EscapeArtist) || p.Has(Signature.Wanderlust) || p.Has(Signature.GuardDog)
            || p.Has(Signature.SeparationAnxiety));

        /// <summary>Strict: the cap while you spur during its sulk after sloppy riding, or null.</summary>
        public double? StrictCap(Personality p, bool mounted, bool spurring, bool sulking) =>
            p != null && p.Has(Signature.Strict) && mounted && spurring && sulking ? _t.StrictRatio : (double?)null;

        /// <summary>Hill crawler: the cap on a steep climb, spurred or not, or null.</summary>
        /// <param name="bribed">Bribable: a loved treat in the last Bribe seconds lifts the cap.</param>
        public double? HillCap(Personality p, bool mounted, double climbSpeed, bool bribed = false) =>
            p != null && p.Has(Signature.HillCrawler) && mounted && climbSpeed >= _t.HillClimbSpeed && !(bribed && p.Has(Signature.Bribable))
                ? _t.HillCrawlRatio : (double?)null;

        /// <summary>Unhurried: the cap while you spur with no danger in Danger memory seconds, or null.</summary>
        public double? UnhurriedCap(Personality p, bool mounted, bool spurring, double secondsSinceDanger) =>
            p != null && p.Has(Signature.Unhurried) && mounted && spurring && secondsSinceDanger > _t.DangerMemorySeconds ? _t.UnhurriedRatio : (double?)null;

        /// <summary>Grudge: it was hurt while you rode it. None when it doesn't hold grudges.</summary>
        public Decision OnHurt(int horse, Personality p, CooldownBook cd, double now)
        {
            if (p != null && p.Has(Signature.Deadpan) && !p.Has(Signature.Grudge) && !p.Has(Signature.Strict))
            {
                cd.Used(horse, BehaviorGroup.Signature, now);
                return new Decision { Action = BehaviorAction.Stare, Group = BehaviorGroup.Signature, Cause = "Deadpan: a flat, unblinking stare at you" };
            }
            if (p == null || !(p.Has(Signature.Grudge) || p.Has(Signature.Strict))) return Decision.None;
            cd.Used(horse, BehaviorGroup.Signature, now);
            string why = p.Has(Signature.Grudge) ? $"Grudge: hurt while you rode it; sulks for {_t.GrudgeSeconds:0} s" : "";
            if (p.Has(Signature.Strict)) why += (why.Length > 0 ? "; " : "") + $"Strict: sloppy riding, no sprinting for {_t.StrictSeconds:0} s";
            return new Decision { Action = p.Has(Signature.Strict) ? BehaviorAction.Snub : BehaviorAction.Agitate, Group = BehaviorGroup.Signature, Cause = why };
        }

        /// <summary>Saddle hater, as you get on. None when it doesn't happen.</summary>
        public Decision OnMounted(int horse, Personality p, CooldownBook cd, double now)
        {
            if (p == null || !p.Has(Signature.SaddleHater) || !Ready(horse, cd, now)) return Decision.None;
            double roll = _random();
            return roll < _t.SaddleHaterChance
                ? Use(horse, BehaviorAction.Agitate, $"Saddle hater: nips at you as you get on: roll {F3(roll)} vs {F3(_t.SaddleHaterChance)}", cd, now)
                : Decision.None;
        }

        /// <summary>Selective hearing, when you whistle and it's not under you. None: it comes as usual.</summary>
        public Decision OnWhistle(int horse, Personality p, bool mounted, CooldownBook cd, double now)
        {
            if (p == null || mounted || !p.Has(Signature.SelectiveHearing) || !Ready(horse, cd, now)) return Decision.None;
            double roll = _random();
            return roll < _t.SelectiveHearingChance
                ? Use(horse, BehaviorAction.Ignore, $"Selective hearing: busy with a weird bush: roll {F3(roll)} vs {F3(_t.SelectiveHearingChance)}", cd, now)
                : Decision.None;
        }

        /// <summary>Jealous: you're giving attention to another horse near it. None when it lets it go.</summary>
        public Decision OnRival(int horse, Personality p, double distance, string what, CooldownBook cd, double now)
        {
            if (p == null || !p.Has(Signature.Jealous) || distance > _t.JealousRange || !Ready(horse, cd, now)) return Decision.None;
            double roll = _random();
            return roll < _t.JealousChance
                ? Use(horse, BehaviorAction.JealousCall, $"Jealous: you {what} another horse {distance:0} m away: roll {F3(roll)} vs {F3(_t.JealousChance)}", cd, now)
                : Decision.None;
        }

        /// <summary>Night: from Night starts hour to Night ends hour (the Behavior tab's own night).</summary>
        public bool IsNight(int hour) =>
            _t.NightStartsHour > _t.NightEndsHour ? hour >= _t.NightStartsHour || hour < _t.NightEndsHour
                                                  : hour >= _t.NightStartsHour && hour < _t.NightEndsHour;

        /// <summary>Dusk and night: from Dusk starts hour until Night ends hour.</summary>
        public bool IsDusk(int hour) =>
            _t.DuskStartsHour > _t.NightEndsHour ? hour >= _t.DuskStartsHour || hour < _t.NightEndsHour
                                                 : hour >= _t.DuskStartsHour && hour < _t.NightEndsHour;

        private static bool Ready(int horse, CooldownBook cd, double now) => cd.Ready(horse, BehaviorGroup.Signature, now);

        private static Decision Use(int horse, BehaviorAction a, string cause, CooldownBook cd, double now)
        {
            cd.Used(horse, BehaviorGroup.Signature, now);
            return new Decision { Action = a, Group = BehaviorGroup.Signature, Cause = cause };
        }

        private static string F3(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
