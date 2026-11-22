using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>How fast a horse learns physical skills (speed, acceleration, handling) and combat skills (gunfire, predators).</summary>
    public struct LearningRate : IEquatable<LearningRate>
    {
        public double Physical { get; }
        public double Combat { get; }

        public LearningRate(double physical, double combat)
        {
            Physical = physical;
            Combat = combat;
        }

        public bool ApproximatelyEquals(LearningRate other, double tolerance = 1e-6) =>
            Math.Abs(Physical - other.Physical) <= tolerance && Math.Abs(Combat - other.Combat) <= tolerance;

        public bool Equals(LearningRate other) => Physical.Equals(other.Physical) && Combat.Equals(other.Combat);
        public override bool Equals(object obj) => obj is LearningRate r && Equals(r);
        public override int GetHashCode() => (Physical.GetHashCode() * 397) ^ Combat.GetHashCode();
        public override string ToString() => $"{Physical:0.###} physical / {Combat:0.###} combat";
    }

    public static class LearningRates
    {
        /// <summary>
        /// The sheet's formula: one class uses its rates; several classes take the best rate of each kind
        /// separately, minus the multi-class penalty. Turkoman (Race + War, 10%): 1.17 physical, 1.35 combat.
        /// </summary>
        public static LearningRate Compute(IReadOnlyList<string> classes, IReadOnlyDictionary<string, LearningRate> classRates, double multiClassPenalty)
        {
            if (classes == null || classes.Count == 0) throw new ArgumentException("A breed needs at least one class.", nameof(classes));
            double physical = double.MinValue, combat = double.MinValue;
            foreach (string c in classes)
            {
                if (!classRates.TryGetValue(c, out LearningRate r)) throw new KeyNotFoundException($"Unknown horse class \"{c}\".");
                physical = Math.Max(physical, r.Physical);
                combat = Math.Max(combat, r.Combat);
            }
            if (classes.Count == 1) return new LearningRate(physical, combat);
            double keep = 1.0 - multiClassPenalty;
            return new LearningRate(physical * keep, combat * keep);
        }
    }
}
