using System.Collections.Generic;
using HorsePersonalities.Core.Json;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>
    /// The roller's tuning numbers, from the breed sheet's Roller tab (rule 6: no tuning numbers in code).
    /// Trait values are on the sheet's -2..+2 scale; stat values are shares of the tier cap.
    /// </summary>
    public sealed class RollerTuning
    {
        /// <summary>Standard deviation of one horse's trait from its breed's leaning.</summary>
        public double TraitSpread { get; }
        /// <summary>How far good horses sit on the good side of the breed, and bad horses on the bad side.</summary>
        public double QualityShift { get; }
        /// <summary>A trait this far on the wrong side of the breed counts as a flaw (or, for a bad horse, a good trait).</summary>
        public double FlawMargin { get; }
        /// <summary>Chance (0-1) of a second quirk.</summary>
        public double SecondQuirkChance { get; }
        /// <summary>Pick weight of a breed's likely quirks against 1 for the others.</summary>
        public double LikelyQuirkWeight { get; }
        /// <summary>Standard deviation of a natural stat, in ranks, before it is rounded to a whole rank.</summary>
        public double StatSpread { get; }
        /// <summary>How far good horses' stats lean up (and bad horses' down), in ranks.</summary>
        public double StatQualityShift { get; }
        /// <summary>Wild horses: Temper minus Bond affinity at or above this breaks Hard.</summary>
        public double HardBreakScore { get; }
        /// <summary>Wild horses: at or above this, Unbreakable if the UnbreakableChance roll also hits.</summary>
        public double UnbreakableBreakScore { get; }

        public RollerTuning(double traitSpread, double qualityShift, double flawMargin, double secondQuirkChance,
            double likelyQuirkWeight, double statSpread, double statQualityShift, double hardBreakScore, double unbreakableBreakScore)
        {
            TraitSpread = traitSpread;
            QualityShift = qualityShift;
            FlawMargin = flawMargin;
            SecondQuirkChance = secondQuirkChance;
            LikelyQuirkWeight = likelyQuirkWeight;
            StatSpread = statSpread;
            StatQualityShift = statQualityShift;
            HardBreakScore = hardBreakScore;
            UnbreakableBreakScore = unbreakableBreakScore;
        }

        /// <summary>JSON key, sheet label, lowest and highest allowed value, for each setting in order.</summary>
        public static readonly IReadOnlyList<(string Key, string Label, double Min, double Max)> Fields = new[]
        {
            ("traitSpread", "Trait spread", 0.01, 2.0),
            ("qualityShift", "Quality shift", 0.0, 2.0),
            ("flawMargin", "Flaw margin", 0.0, 2.0),
            ("secondQuirkChance", "Second quirk chance", 0.0, 1.0),
            ("likelyQuirkWeight", "Likely quirk weight", 1.0, 100.0),
            ("statSpread", "Stat spread", 0.01, 3.0),
            ("statQualityShift", "Stat quality shift", 0.0, 2.0),
            ("hardBreakScore", "Hard break score", -4.0, 4.0),
            ("unbreakableBreakScore", "Unbreakable break score", -4.0, 4.0),
        };

        internal static RollerTuning Read(JsonValue j, List<string> problems)
        {
            if (j.Kind != JsonKind.Object)
            {
                problems.Add("\"roller\" must be an object with the Roller tab's settings.");
                return null;
            }
            var v = new double[Fields.Count];
            for (int i = 0; i < Fields.Count; i++)
            {
                var f = Fields[i];
                double x = j.GetNumber(f.Key, double.NaN);
                if (double.IsNaN(x)) problems.Add($"roller.{f.Key} ({f.Label}) is missing.");
                else if (x < f.Min || x > f.Max) problems.Add($"roller.{f.Key} ({f.Label}) = {x} is outside {f.Min} to {f.Max}.");
                v[i] = x;
            }
            if (!double.IsNaN(v[7]) && !double.IsNaN(v[8]) && v[8] < v[7])
                problems.Add("roller: Unbreakable break score must not be below Hard break score.");
            return new RollerTuning(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8]);
        }
    }

    /// <summary>
    /// How many whole ranks a natural stat may move up and down from the breed baseline in one tier (sheet's Tiers
    /// tab). Game stats are ranks 0 to 9, one stat-bar segment each.
    /// </summary>
    public sealed class TierCap
    {
        public StatTier Tier { get; }
        /// <summary>Most ranks a stat may rise.</summary>
        public int Up { get; }
        /// <summary>Most ranks a stat may drop.</summary>
        public int Down { get; }

        public TierCap(StatTier tier, int up, int down)
        {
            Tier = tier;
            Up = up;
            Down = down;
        }

        /// <summary>The game's highest stat rank.</summary>
        public const int MaxRank = 9;
    }
}
