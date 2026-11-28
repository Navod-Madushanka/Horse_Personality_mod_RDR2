using System;
using System.Globalization;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// Phase 6: decides, every check, whether a horse spooks, bucks, refuses or kicks, from its traits and what is
    /// happening (Build plan: Phase 6, decided Nov 26, 2026). Pure: the game layer builds the <see cref="HorseSituation"/>,
    /// asks for a <see cref="Decision"/>, plays it and logs the cause.
    ///
    /// In order, first match wins:
    /// 1. Threat (predator, gunfire, explosion): spook chance falls with Courage (logistic, Courage slope).
    ///    Ridden: Temper at or above Scared buck temper throws you; Courage below Rear courage rears; else agitated.
    ///    Not ridden: Courage below Bolt courage bolts away from it; else watches it.
    ///    Not spooked but timid (Courage below 0): a tell, looking at the threat.
    /// 2. Pushed (ridden): buck or refuse with Push buck chance x e^(Temper slope x Temper), up to the max.
    ///    Temper 1 or more throws you; below that a refusal (quick stop).
    /// 3. Random spook from Nerves: rate per minute = base x e^(Nerves slope x Nerves) x night x gallop x bond x SpookFrequency.
    ///    Ridden at a gallop: skid; ridden slower: rear (timid) or agitated; not ridden: agitated.
    /// 4. Kick: you lingered behind it; Kick chance x e^(Temper slope x Temper).
    /// </summary>
    public sealed class BehaviorBrain
    {
        private readonly BehaviorTuning _t;
        private readonly double _spookFrequency;
        private readonly Func<double> _random;

        /// <param name="random">Uniform [0, 1). Behaviors are not seeded: they should differ every ride.</param>
        public BehaviorBrain(BehaviorTuning tuning, ModSettings settings, Func<double> random)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _spookFrequency = settings?.SpookFrequency ?? 1.0;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public Decision Decide(int horse, Personality p, HorseSituation s, double dtSeconds, CooldownBook cooldowns, double now)
        {
            if (p == null || (!s.Mounted && !s.PlayerNear)) return Decision.None;
            Traits t = p.Traits;

            // 1. Threats.
            // Wolf brawler (coat signature): a predator is something to fight, not flee, when you're off it.
            if (s.Threat == Threat.Predator && p.Has(Breeds.Signature.WolfBrawler) && s.ThreatEntity != 0)
            {
                if (s.Mounted) return cooldowns.Ready(horse, BehaviorGroup.Tell, now)
                    ? Use(horse, BehaviorAction.LookAtThreat, BehaviorGroup.Tell, "Wolf brawler: eyes the predator, unafraid", cooldowns, now)
                    : Decision.None;
                if (cooldowns.Ready(horse, BehaviorGroup.Signature, now))
                    return Use(horse, BehaviorAction.Brawl, BehaviorGroup.Signature, "Wolf brawler: goes for the predator", cooldowns, now);
                return Decision.None;
            }
            // War horse (coat signature): in a fight it neither spooks nor tells.
            bool battle = s.InFight && p.Has(Breeds.Signature.WarHorse);
            if (s.Threat != Threat.None && !battle)
            {
                // Phase 8: a skill eases fear only in its own situation.
                double learned = s.Threat == Threat.Predator ? s.PredatorSkillCourage : s.GunfireSkillCourage;
                double courage = t.Courage + learned - (s.Threat == Threat.Explosion ? _t.ExplosionCouragePenalty : 0);
                double chance = ThreatChance(courage) * (s.Threat == Threat.RiderGunfire ? _t.RiderGunfireScale : 1.0);
                if (cooldowns.Ready(horse, BehaviorGroup.Spook, now))
                {
                    double roll = _random();
                    string why = $"{s.Threat}, Courage {F(t.Courage)}{(learned > 0 ? $" + skill {F(learned)}" : "")}: roll {F3(roll)} vs {F3(chance)}";
                    if (roll < chance)
                    {
                        BehaviorAction a;
                        BehaviorGroup g = BehaviorGroup.Spook;
                        if (s.Mounted)
                        {
                            if (t.Temper >= _t.ScaredBuckTemper && cooldowns.Ready(horse, BehaviorGroup.Buck, now)) { a = BehaviorAction.ThrowRider; g = BehaviorGroup.Buck; why += $", Temper {F(t.Temper)} bucks instead of rearing"; }
                            else a = t.Courage < _t.RearCourage ? BehaviorAction.Rear : BehaviorAction.Agitate;
                        }
                        else a = t.Courage < _t.BoltCourage ? BehaviorAction.Bolt : BehaviorAction.LookAtThreat;
                        return Use(horse, a, g, "scared: " + why, cooldowns, now);
                    }
                }
                if (t.Courage < 0 && cooldowns.Ready(horse, BehaviorGroup.Tell, now))
                    return Use(horse, BehaviorAction.LookAtThreat, BehaviorGroup.Tell, $"tell: {s.Threat}, timid (Courage {F(t.Courage)})", cooldowns, now);
            }

            // 2. Pushed while ridden.
            if (s.Mounted && s.Pushed != PushKind.None && cooldowns.Ready(horse, BehaviorGroup.Buck, now))
            {
                double chance = Math.Min(_t.MaxPushBuckChance, _t.PushBuckChance * Math.Exp(_t.TemperSlope * t.Temper));
                double roll = _random();
                if (roll < chance)
                {
                    BehaviorAction a = t.Temper >= 1 ? BehaviorAction.ThrowRider : BehaviorAction.QuickStop;
                    return Use(horse, a, BehaviorGroup.Buck, $"pushed ({s.Pushed}), Temper {F(t.Temper)}: roll {F3(roll)} vs {F3(chance)}", cooldowns, now);
                }
            }

            // 3. Random spooks from Nerves.
            if (!battle && cooldowns.Ready(horse, BehaviorGroup.Spook, now) && dtSeconds > 0)
            {
                double perMinute = RandomSpookRate(t.Nerves, s, out string factors, p);
                double chance = 1 - Math.Exp(-perMinute * dtSeconds / 60.0);
                double roll = _random();
                if (roll < chance)
                {
                    BehaviorAction a;
                    if (!s.Mounted) a = BehaviorAction.Agitate;
                    else if (s.SpeedMs >= _t.GallopSpeed) a = BehaviorAction.Skid;
                    else a = t.Courage < _t.RearCourage ? BehaviorAction.Rear : BehaviorAction.Agitate;
                    return Use(horse, a, BehaviorGroup.Spook, $"spooked at nothing: Nerves {F(t.Nerves)}{factors}, {F3(perMinute)}/min", cooldowns, now);
                }
            }

            // 4. Kick when you linger behind it.
            if (s.PlayerLingeredBehind && !s.Mounted && cooldowns.Ready(horse, BehaviorGroup.Kick, now))
            {
                double chance = Math.Min(1.0, _t.KickChance * Math.Exp(_t.TemperSlope * t.Temper));
                double roll = _random();
                if (roll < chance)
                    return Use(horse, BehaviorAction.Kick, BehaviorGroup.Kick, $"you lingered behind it, Temper {F(t.Temper)}: roll {F3(roll)} vs {F3(chance)}", cooldowns, now);
            }
            return Decision.None;
        }

        /// <summary>Chance to spook at a threat: logistic in Courage, Threat spook chance at Courage 0.</summary>
        public double ThreatChance(double courage)
        {
            double b = _t.ThreatSpookChance;
            return 1.0 / (1.0 + (1 - b) / b * Math.Exp(_t.CourageSlope * courage));
        }

        public double RandomSpookRate(double nerves, HorseSituation s, out string factors, Personality p = null)
        {
            double rate = _t.SpookBaseRate * Math.Exp(_t.NervesSlope * nerves) * _spookFrequency;
            factors = "";
            if (IsNight(s.GameHour))
            {
                if (p != null && p.Has(Breeds.Signature.NightOwl)) { rate *= _t.NightOwlMultiplier; factors += ", night (Night owl: calmer)"; }
                else { rate *= _t.NightSpookMultiplier; factors += ", night"; }
            }
            if (s.InTown && p != null && p.Has(Breeds.Signature.CityLover)) { rate *= _t.CityLoverMultiplier; factors += ", town (City lover)"; }
            if (s.Mounted && s.SpeedMs >= _t.GallopSpeed) { rate *= _t.GallopSpookMultiplier; factors += ", gallop"; }
            if (s.BondLevel <= 1) { rate *= _t.WeakBondSpookMultiplier; factors += ", weak bond"; }
            else if (s.BondLevel >= 4) { rate *= _t.FullBondSpookMultiplier; factors += ", full bond"; }
            return rate;
        }

        public bool IsNight(int hour) =>
            _t.NightStartsHour > _t.NightEndsHour ? hour >= _t.NightStartsHour || hour < _t.NightEndsHour
                                                  : hour >= _t.NightStartsHour && hour < _t.NightEndsHour;

        private static Decision Use(int horse, BehaviorAction a, BehaviorGroup g, string cause, CooldownBook cd, double now)
        {
            cd.Used(horse, g, now);
            return new Decision { Action = a, Group = g, Cause = cause };
        }

        private static string F(double v) => v.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
        private static string F3(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
