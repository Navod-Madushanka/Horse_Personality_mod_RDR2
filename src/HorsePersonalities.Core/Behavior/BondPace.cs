using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// How fast a horse comes to trust you (Build plan: Trust): Bond affinity (e^(slope x affinity)), the slower climb
    /// from level 3 to 4 for good and prized horses, and slower for a wild horse that broke Hard.
    /// </summary>
    public sealed class BondPace
    {
        private readonly BehaviorTuning _b;
        private readonly RollerTuning _r;

        public BondPace(BehaviorTuning behavior, RollerTuning roller)
        {
            _b = behavior;
            _r = roller;
        }

        /// <param name="level">The horse's trust level (1-4) before the gain.</param>
        public double Multiplier(Personality p, int level)
        {
            double m = Math.Exp(_b.BondAffinitySlope * p.Traits.BondAffinity);
            if (level == 3)
            {
                if (p.Prized) m *= _r.PrizedBond4Speed;
                else if (p.Quality == HorseQuality.Good) m *= _r.GoodBond4Speed;
            }
            if (p.Breaking == BreakingDifficulty.Hard) m *= _b.HardBondSpeed;
            return m;
        }
    }

    /// <summary>Trust levels 1-4 from trust points, on the game's own bond thresholds for the horse's model.</summary>
    public static class TrustLevels
    {
        /// <summary>
        /// The points needed for levels 2, 3 and 4 (the game's GET_DEFAULT_ATTRIBUTE_POINTS_NEEDED_FOR_RANK, cumulative:
        /// 350, 950, 1950 for a Dutch Warmblood in game). Null if they don't make a usable scale.
        /// </summary>
        public static int[] Valid(int level2, int level3, int level4) =>
            level2 > 0 && level3 > level2 && level4 > level3 ? new[] { level2, level3, level4 } : null;

        public static int Level(double points, IReadOnlyList<int> needed)
        {
            int level = 1;
            for (int i = 0; i < needed.Count; i++)
                if (points >= needed[i]) level = i + 2;
            return level;
        }
    }

    public enum TrustUpdateKind { None, Started, Gained, FromOutside, GameRestored }

    public struct TrustUpdate
    {
        public TrustUpdateKind Kind;
        /// <summary>Bond XP the game gave (or took, for GameRestored).</summary>
        public int BondChange;
        /// <summary>Trust added.</summary>
        public double Added;
        public double Multiplier;
        public int LevelBefore, Level;
        /// <summary>Worth writing horses.json: trust started, a level changed, an outside jump, or enough unsaved gain.</summary>
        public bool Save;
    }

    /// <summary>
    /// The mod's own bond (Build plan: Trust, decided Nov 28, 2026). The game's bond XP is never written: a script's change
    /// doesn't reach the game's save (home test Stage 13). Instead every rise in the game's bond XP is scaled by the
    /// horse's <see cref="BondPace"/> and added to its trust, which horses.json keeps. A drop (the game putting its saved
    /// value back after a load) is never counted; a jump bigger than bonding ever gives (a trainer, DevTools trigger 42)
    /// is added as it is.
    /// </summary>
    public sealed class TrustTracker
    {
        /// <summary>The largest natural gain seen in game was +35; anything above this was set from outside.</summary>
        public const int MaxNaturalGain = 100;
        /// <summary>Unsaved trust worth a write (natural riding gives about 24 a minute; a pat 15 to 35).</summary>
        public const double SaveEveryPoints = 50;

        private readonly BondPace _pace;
        private readonly Dictionary<int, int> _seen = new Dictionary<int, int>();
        private readonly Dictionary<int, double> _unsaved = new Dictionary<int, double>();

        public TrustTracker(BondPace pace) { _pace = pace ?? throw new ArgumentNullException(nameof(pace)); }

        /// <param name="horse">The horse's handle this session (a reload gives new handles, so it starts from the game's value).</param>
        /// <param name="needed">The trust scale (<see cref="TrustLevels.Valid"/>); null: levels can't be told, every gain is scaled as level 1-2.</param>
        public TrustUpdate Update(int horse, HorseRecord record, int bondPoints, IReadOnlyList<int> needed)
        {
            var u = new TrustUpdate();
            if (record == null || record.Status != PersonalityStatus.Permanent) return u;
            if (!record.Trust.HasValue)
            {
                // A horse saved before trust, or one that just became permanent: it keeps the bond it has.
                record.Trust = Math.Max(0, bondPoints);
                _seen[horse] = bondPoints;
                u.Kind = TrustUpdateKind.Started;
                u.Level = u.LevelBefore = needed != null ? TrustLevels.Level(record.Trust.Value, needed) : 0;
                u.Save = true;
                return u;
            }
            if (!_seen.TryGetValue(horse, out int before))
            {
                _seen[horse] = bondPoints;
                return u;
            }
            _seen[horse] = bondPoints;
            int change = bondPoints - before;
            if (change == 0) return u;
            u.BondChange = change;
            double trust = record.Trust.Value;
            u.LevelBefore = needed != null ? TrustLevels.Level(trust, needed) : 0;
            if (change < 0)
            {
                u.Kind = TrustUpdateKind.GameRestored;
                u.Level = u.LevelBefore;
                return u;
            }
            if (change > MaxNaturalGain)
            {
                u.Kind = TrustUpdateKind.FromOutside;
                u.Multiplier = 1;
                u.Added = change;
            }
            else
            {
                u.Kind = TrustUpdateKind.Gained;
                u.Multiplier = _pace.Multiplier(record.Personality, Math.Max(1, u.LevelBefore));
                u.Added = change * u.Multiplier;
            }
            record.Trust = trust + u.Added;
            u.Level = needed != null ? TrustLevels.Level(record.Trust.Value, needed) : 0;
            double unsaved = (_unsaved.TryGetValue(horse, out double s) ? s : 0) + u.Added;
            u.Save = u.Kind == TrustUpdateKind.FromOutside || u.Level != u.LevelBefore || unsaved >= SaveEveryPoints;
            _unsaved[horse] = u.Save ? 0 : unsaved;
            return u;
        }
    }
}
