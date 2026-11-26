using System;
using System.Collections.Generic;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Identity;

namespace HorsePersonalities.Core.Stats
{
    /// <summary>One base-rank change for the game layer to make.</summary>
    public struct StatChange
    {
        public PedAttribute Attribute;
        public int From;
        public int To;
        public int Vanilla;
        public int Natural;
        public int Revealed;

        public override string ToString() =>
            $"{Attribute} base rank {From} -> {To} (original {Vanilla}, natural {Natural:+0;-0;0}, showing {Revealed:+0;-0;0})";
    }

    /// <summary>
    /// Phase 5 (Build plan: Phase 5): health and stamina natural stats shown on the game's stat bars as the bond grows.
    /// The mod changes only the stat's base rank; the bonus rank (yellow segments, from tack) stays the game's.
    /// Pure logic: the game layer reads the attributes, calls <see cref="Plan"/>, and writes back what it returns.
    /// </summary>
    public static class StatPlanner
    {
        public const string Health = "health";
        public const string Stamina = "stamina";

        /// <summary>
        /// How much of a natural difference shows at this bond level: the first rank at <paramref name="firstRankBond"/>,
        /// the second at <paramref name="secondRankBond"/>. Nothing more ever shows: natural stats are at most 2 ranks.
        /// </summary>
        public static int Revealed(int natural, int bondLevel, int firstRankBond, int secondRankBond)
        {
            if (natural == 0) return 0;
            int visible = (bondLevel >= firstRankBond ? 1 : 0) + (bondLevel >= secondRankBond ? 1 : 0);
            int size = Math.Min(Math.Abs(natural), visible);
            return natural > 0 ? size : -size;
        }

        /// <summary>
        /// The base-rank changes needed now. The first time a stat is seen, its current base rank is stored as the
        /// horse's original (reported in <paramref name="learned"/>, for saving) before anything is changed.
        /// With <paramref name="restore"/> (RestoreVanillaStats), every stat goes back to its stored original instead.
        /// Only permanent personalities get stat changes.
        /// </summary>
        public static List<StatChange> Plan(HorseRecord record, int bondLevel, AttributeReading health, AttributeReading stamina,
            int firstRankBond, int secondRankBond, bool restore, out List<(string Stat, int Base)> learned)
        {
            var changes = new List<StatChange>();
            var found = new List<(string Stat, int Base)>();
            learned = found;
            if (record == null || record.Status != PersonalityStatus.Permanent) return changes;

            void One(string stat, AttributeReading reading, int natural)
            {
                if (!record.VanillaBase.TryGetValue(stat, out int vanilla))
                {
                    if (restore) return; // never changed by the mod, so nothing to restore
                    vanilla = reading.BaseRank;
                    found.Add((stat, vanilla));
                }
                int shown = restore ? 0 : Revealed(natural, bondLevel, firstRankBond, secondRankBond);
                int max = reading.MaxRank > 0 ? reading.MaxRank : Breeds.TierCap.MaxRank;
                int target = Math.Max(0, Math.Min(max, vanilla + shown));
                if (reading.BaseRank != target)
                    changes.Add(new StatChange { Attribute = reading.Attribute, From = reading.BaseRank, To = target, Vanilla = vanilla, Natural = natural, Revealed = shown });
            }

            One(Health, health, record.Personality.NaturalStats.Health);
            One(Stamina, stamina, record.Personality.NaturalStats.Stamina);
            return changes;
        }
    }
}
