using System;
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
        private readonly Dictionary<string, double> _v;

        /// <summary>Standard deviation of one horse's trait from its breed's leaning.</summary>
        public double TraitSpread => _v["traitSpread"];
        /// <summary>How far good horses sit on the good side of the breed, and bad horses on the bad side.</summary>
        public double QualityShift => _v["qualityShift"];
        /// <summary>A trait this far on the wrong side of the breed counts as a flaw (or, for a bad horse, a good trait).</summary>
        public double FlawMargin => _v["flawMargin"];
        /// <summary>Chance (0-1) of a second quirk.</summary>
        public double SecondQuirkChance => _v["secondQuirkChance"];
        /// <summary>Pick weight of a breed's likely quirks against 1 for the others.</summary>
        public double LikelyQuirkWeight => _v["likelyQuirkWeight"];
        /// <summary>Standard deviation of a natural stat, in ranks, before it is rounded to a whole rank.</summary>
        public double StatSpread => _v["statSpread"];
        /// <summary>How far good horses' stats lean up (and bad horses' down), in ranks.</summary>
        public double StatQualityShift => _v["statQualityShift"];
        /// <summary>Wild horses: Temper minus Bond affinity at or above this breaks Hard.</summary>
        public double HardBreakScore => _v["hardBreakScore"];
        /// <summary>Wild horses: at or above this, Unbreakable if the UnbreakableChance roll also hits.</summary>
        public double UnbreakableBreakScore => _v["unbreakableBreakScore"];
        /// <summary>Bond XP rate from bond 3 to 4 for a good horse that isn't prized (1 = vanilla pace).</summary>
        public double GoodBond4Speed => _v["goodBond4Speed"];
        /// <summary>Bond XP rate from bond 3 to 4 for a prized horse.</summary>
        public double PrizedBond4Speed => _v["prizedBond4Speed"];
        /// <summary>Added to a prized wild horse's breaking score. Prized horses are never Unbreakable.</summary>
        public double PrizedBreakScoreBonus => _v["prizedBreakScoreBonus"];
        /// <summary>Bond level at which the first rank of a health or stamina difference shows.</summary>
        public int RevealFirstRankBond => (int)_v["revealFirstRankBond"];
        /// <summary>Bond level at which a second rank of difference shows.</summary>
        public int RevealSecondRankBond => (int)_v["revealSecondRankBond"];

        private RollerTuning(Dictionary<string, double> values)
        {
            _v = values;
        }

        /// <summary>Tuning with every value given; for tests.</summary>
        public static RollerTuning Of(IDictionary<string, double> values)
        {
            var d = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields) d[f.Key] = values.TryGetValue(f.Key, out double v) ? v : throw new ArgumentException("Missing " + f.Key);
            return new RollerTuning(d);
        }

        /// <summary>JSON key, sheet label, lowest and highest allowed value, and whether it must be whole, for each setting.</summary>
        public static readonly IReadOnlyList<(string Key, string Label, double Min, double Max, bool Whole)> Fields = new[]
        {
            ("traitSpread", "Trait spread", 0.01, 2.0, false),
            ("qualityShift", "Quality shift", 0.0, 2.0, false),
            ("flawMargin", "Flaw margin", 0.0, 2.0, false),
            ("secondQuirkChance", "Second quirk chance", 0.0, 1.0, false),
            ("likelyQuirkWeight", "Likely quirk weight", 1.0, 100.0, false),
            ("statSpread", "Stat spread", 0.01, 3.0, false),
            ("statQualityShift", "Stat quality shift", 0.0, 2.0, false),
            ("hardBreakScore", "Hard break score", -4.0, 4.0, false),
            ("unbreakableBreakScore", "Unbreakable break score", -4.0, 4.0, false),
            ("goodBond4Speed", "Good bond 4 speed", 0.05, 2.0, false),
            ("prizedBond4Speed", "Prized bond 4 speed", 0.05, 2.0, false),
            ("prizedBreakScoreBonus", "Prized break score bonus", 0.0, 4.0, false),
            ("revealFirstRankBond", "Reveal first rank bond", 1.0, 4.0, true),
            ("revealSecondRankBond", "Reveal second rank bond", 1.0, 4.0, true),
        };

        internal static RollerTuning Read(JsonValue j, List<string> problems)
        {
            if (j.Kind != JsonKind.Object)
            {
                problems.Add("\"roller\" must be an object with the Roller tab's settings.");
                return null;
            }
            var v = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fields)
            {
                double x = j.GetNumber(f.Key, double.NaN);
                if (double.IsNaN(x)) problems.Add($"roller.{f.Key} ({f.Label}) is missing.");
                else if (x < f.Min || x > f.Max) problems.Add($"roller.{f.Key} ({f.Label}) = {x} is outside {f.Min} to {f.Max}.");
                else if (f.Whole && x != Math.Floor(x)) problems.Add($"roller.{f.Key} ({f.Label}) = {x} must be a whole number.");
                v[f.Key] = x;
            }
            if (v["unbreakableBreakScore"] < v["hardBreakScore"])
                problems.Add("roller: Unbreakable break score must not be below Hard break score.");
            if (v["revealSecondRankBond"] < v["revealFirstRankBond"])
                problems.Add("roller: Reveal second rank bond must not be below Reveal first rank bond.");
            return new RollerTuning(v);
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
