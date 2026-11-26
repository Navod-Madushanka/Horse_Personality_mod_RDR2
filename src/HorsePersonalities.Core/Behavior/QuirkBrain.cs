using System;
using System.Globalization;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>What the quirks and demeanor need to know about one horse, built by the game layer each check.</summary>
    public struct QuirkSituation
    {
        public bool Mounted;
        public bool PlayerNear;
        public double PlayerDistance;
        public double StandingStillSeconds;
        public bool OtherHorseNear;
        /// <summary>A person or animal within the Nosy range to watch; 0 if none.</summary>
        public int PasserBy;
        /// <summary>An NPC standing right behind the horse; 0 if none.</summary>
        public int NpcBehind;
        /// <summary>Other people within the Kicker witness range, not counting the NPC behind it.</summary>
        public int Witnesses;
        /// <summary>The player just walked past the Shadow trigger distance (reported once per walk-away).</summary>
        public bool PlayerWalkedAway;
        public int DirtRank;
    }

    /// <summary>
    /// Phase 7 (Build plan, decided Nov 26, 2026): a horse's quirks and its breed's demeanor. Quirks share one cooldown
    /// per horse (QuirkCooldownSeconds); demeanor uses the tell cooldown. First match wins; rates are per minute and
    /// become a chance per check as 1 - e^(-rate x dt / 60).
    /// </summary>
    public sealed class QuirkBrain
    {
        private readonly BehaviorTuning _t;
        private readonly Func<double> _random;

        public QuirkBrain(BehaviorTuning tuning, Func<double> random)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public Decision Decide(int horse, Personality p, DemeanorTraits demeanor, QuirkSituation s, double dt, CooldownBook cd, double now)
        {
            if (p == null || (!s.Mounted && !s.PlayerNear)) return Decision.None;
            bool still = s.StandingStillSeconds > 0;

            if (cd.Ready(horse, BehaviorGroup.Quirk, now))
            {
                // Events first: they happen once, rates can wait.
                if (Has(p, Quirk.Shadow) && s.PlayerWalkedAway && !s.Mounted)
                    return Use(horse, BehaviorAction.Follow, BehaviorGroup.Quirk, "Shadow: you walked away", 0, cd, now);
                if (Has(p, Quirk.Wanderer) && !s.Mounted && s.StandingStillSeconds >= _t.WandererDelaySeconds && s.PlayerDistance >= _t.WandererPlayerDistance)
                    return Use(horse, BehaviorAction.Wander, BehaviorGroup.Quirk, $"Wanderer: left standing {s.StandingStillSeconds:0} s with you {s.PlayerDistance:0} m away", 0, cd, now);
                if (Has(p, Quirk.Kicker) && !s.Mounted && still && s.NpcBehind != 0 && s.Witnesses == 0)
                {
                    double roll = _random();
                    if (roll < _t.KickerChance)
                        return Use(horse, BehaviorAction.KickNpc, BehaviorGroup.Quirk, $"Kicker: someone behind it, no witnesses: roll {F3(roll)} vs {F3(_t.KickerChance)}", s.NpcBehind, cd, now);
                }
                if (Has(p, Quirk.HeadTosser) && still && Chance(_t.HeadTosserRate, dt))
                    return Use(horse, BehaviorAction.Agitate, BehaviorGroup.Quirk, "Head-tosser: standing still", 0, cd, now);
                if (Has(p, Quirk.Nosy) && s.PasserBy != 0 && Chance(_t.NosyRate, dt))
                    return Use(horse, BehaviorAction.LookAt, BehaviorGroup.Quirk, "Nosy: someone passing", s.PasserBy, cd, now);
                if (Has(p, Quirk.Chatterbox) && Chance(_t.ChatterboxRate * (s.OtherHorseNear ? _t.ChatterboxHorseNearMultiplier : 1), dt))
                    return Use(horse, _random() < 0.5 ? BehaviorAction.Call : BehaviorAction.Snort, BehaviorGroup.Quirk,
                        "Chatterbox" + (s.OtherHorseNear ? ": another horse near" : ""), 0, cd, now);
            }

            if (demeanor != null && cd.Ready(horse, BehaviorGroup.Tell, now))
            {
                if (demeanor.Impatient > 0 && s.StandingStillSeconds >= _t.ImpatientAfterSeconds && Chance(_t.ImpatientRate * demeanor.Impatient, dt))
                    return Use(horse, BehaviorAction.Agitate, BehaviorGroup.Tell, $"demeanor Impatient {demeanor.Impatient:0.#}: kept standing {s.StandingStillSeconds:0} s", 0, cd, now);
                if (demeanor.Fussy > 0 && s.DirtRank >= _t.FussyDirtRank && Chance(_t.FussyRate * demeanor.Fussy, dt))
                    return Use(horse, BehaviorAction.Snort, BehaviorGroup.Tell, $"demeanor Fussy {demeanor.Fussy:0.#}: dirty (rank {s.DirtRank})", 0, cd, now);
                if (demeanor.Alert > 0 && s.PasserBy != 0 && Chance(_t.AlertRate * demeanor.Alert, dt))
                    return Use(horse, BehaviorAction.LookAt, BehaviorGroup.Tell, $"demeanor Alert {demeanor.Alert:0.#}: watching", s.PasserBy, cd, now);
            }
            return Decision.None;
        }

        private bool Chance(double perMinute, double dt) => perMinute > 0 && dt > 0 && _random() < 1 - Math.Exp(-perMinute * dt / 60.0);

        private static bool Has(Personality p, Quirk q) => p.Quirks.Contains(q);

        private static Decision Use(int horse, BehaviorAction a, BehaviorGroup g, string cause, int target, CooldownBook cd, double now)
        {
            cd.Used(horse, g, now);
            return new Decision { Action = a, Group = g, Cause = cause, Target = target };
        }

        private static string F3(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
