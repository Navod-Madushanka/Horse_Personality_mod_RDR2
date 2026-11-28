using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>What the admiration check sees of the horse (Phase 8b).</summary>
    public struct AdmirationSituation
    {
        public int BondLevel;
        public int HealthCore;
        public int StaminaCore;
        public int DirtRank;
        public bool Mounted;
        /// <summary>The horse's speed, m/s.</summary>
        public double Speed;
        /// <summary>On foot: how far you are from the horse (m). Ignored when mounted.</summary>
        public double PlayerDistance;
    }

    /// <summary>A person near the horse who might compliment it.</summary>
    public struct Admirer
    {
        public int Ped;
        /// <summary>Metres from the horse.</summary>
        public double Distance;
        public bool HasNiceHorse;
        public bool HasGoodCondition;
        public bool Speaking;
        /// <summary>In combat with you, or hogtied: no compliments.</summary>
        public bool Unfriendly;
    }

    /// <summary>
    /// Phase 8b (Navod, Nov 28, 2026): people compliment any horse at bond 4 that is clean and fit, as the game does for
    /// its fine breeds. Decides whether the horse qualifies, and who says what; the game layer looks and speaks.
    /// </summary>
    public sealed class AdmirationBrain
    {
        /// <summary>The game's own compliment, which 364 voices have.</summary>
        public const string NiceHorse = "NICE_HORSE";
        /// <summary>The gang's line in camp ("horse looks in good condition"); preferred when the person has it.</summary>
        public const string GoodCondition = "PLAYER_HORSE_IN_GOOD_CONDITION";

        private readonly BehaviorTuning _t;
        private readonly Func<double> _random;
        private readonly Dictionary<int, double> _complimentedBy = new Dictionary<int, double>();
        private double _last = double.NegativeInfinity;

        public AdmirationBrain(BehaviorTuning tuning, Func<double> random)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Why the horse doesn't earn compliments right now, or null when it does (bond, fitness, cleanliness).</summary>
        public string WhyNotAdmired(AdmirationSituation s)
        {
            if (s.BondLevel < _t.AdmireBond) return $"bond {s.BondLevel} < {_t.AdmireBond}";
            if (s.HealthCore < _t.AdmireMinCore || s.StaminaCore < _t.AdmireMinCore) return $"cores {s.HealthCore}/{s.StaminaCore} < {_t.AdmireMinCore}";
            if (s.DirtRank > _t.AdmireMaxDirtRank) return $"dirt rank {s.DirtRank} > {_t.AdmireMaxDirtRank}";
            return null;
        }

        /// <summary>You're with the horse slowly enough for people to take a look: riding at a walk or slow trot, or on foot beside it.</summary>
        public bool OnShow(AdmirationSituation s) =>
            s.Mounted ? s.Speed < _t.AdmireMaxSpeed : s.PlayerDistance <= _t.AdmireBesideDistance;

        /// <summary>True when the cooldown is over, so the game layer can skip the people scan until then.</summary>
        public bool Ready(double now) => now - _last >= _t.AdmireCooldown;

        /// <summary>
        /// Picks who compliments the horse this check, and the line they say; Ped 0 when nobody does.
        /// <paramref name="dt"/> is the time since the last check (s), for the per-minute rate.
        /// </summary>
        public (int Ped, string Line) Pick(AdmirationSituation s, IReadOnlyList<Admirer> people, double now, double dt)
        {
            if (!Ready(now) || WhyNotAdmired(s) != null || !OnShow(s) || people == null) return (0, null);
            Forget(now);
            Admirer best = default;
            bool found = false;
            foreach (Admirer a in people)
            {
                if (a.Ped == 0 || a.Unfriendly || a.Speaking || a.Distance > _t.AdmireRange) continue;
                if (!a.HasNiceHorse && !a.HasGoodCondition) continue;
                if (_complimentedBy.ContainsKey(a.Ped)) continue;
                if (!found || a.Distance < best.Distance) { best = a; found = true; }
            }
            if (!found) return (0, null);
            if (!(_t.AdmireRate > 0 && dt > 0 && _random() < 1 - Math.Exp(-_t.AdmireRate * dt / 60.0))) return (0, null);
            _last = now;
            _complimentedBy[best.Ped] = now;
            return (best.Ped, best.HasGoodCondition ? GoodCondition : NiceHorse);
        }

        private void Forget(double now)
        {
            if (_complimentedBy.Count == 0) return;
            double keep = _t.AdmireSamePersonMinutes * 60;
            var old = new List<int>();
            foreach (var kv in _complimentedBy) if (now - kv.Value >= keep) old.Add(kv.Key);
            foreach (int ped in old) _complimentedBy.Remove(ped);
        }
    }
}
