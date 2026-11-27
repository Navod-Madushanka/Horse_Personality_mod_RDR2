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

        public SignatureBrain(BehaviorTuning tuning, Func<double> random)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Lazy pace: the move blend ratio to cap it at while you aren't spurring, or null for no cap.</summary>
        public double? LazyCap(Personality p, bool mounted, bool spurring) =>
            p != null && p.Has(Signature.LazyPace) && mounted && !spurring ? _t.LazyWalkRatio : (double?)null;

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
            if (far) return Decision.None; // only the habits above work with you far away
            if (p.Has(Signature.BoredBuck) && s.Mounted && s.StandingStillSeconds > 0 && s.AimingSeconds >= _t.BoredAimSeconds
                && !_boredThisAim.Contains(horse) && Ready(horse, cooldowns, now))
            {
                _boredThisAim.Add(horse);
                double roll = _random();
                if (roll < _t.BoredBuckChance)
                    return Use(horse, s.BondLevel <= 2 ? BehaviorAction.ThrowRider : BehaviorAction.Agitate,
                        $"Bored buck: you aimed {s.AimingSeconds:0} s, bond {s.BondLevel}: roll {F3(roll)} vs {F3(_t.BoredBuckChance)}", cooldowns, now);
            }
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
        public static bool WatchedFar(Personality p) => p != null && (p.Has(Signature.EscapeArtist) || p.Has(Signature.Wanderlust) || p.Has(Signature.GuardDog));

        /// <summary>Unhurried: the cap while you spur with no danger in Danger memory seconds, or null.</summary>
        public double? UnhurriedCap(Personality p, bool mounted, bool spurring, double secondsSinceDanger) =>
            p != null && p.Has(Signature.Unhurried) && mounted && spurring && secondsSinceDanger > _t.DangerMemorySeconds ? _t.UnhurriedRatio : (double?)null;

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
