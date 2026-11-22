namespace HorsePersonalities.Core.Breeds
{
    /// <summary>
    /// A breed's average leanings on the sheet's -2..+2 scale. Higher Courage = braver, Nerves = jumpier,
    /// Temper = shorter fuse, Bond affinity = bonds faster. Leanings shift the roll's odds; they never lock it.
    /// </summary>
    public sealed class TraitLeans
    {
        public const double Min = -2.0;
        public const double Max = 2.0;

        public double Courage { get; }
        public double Nerves { get; }
        public double Temper { get; }
        public double BondAffinity { get; }

        public TraitLeans(double courage, double nerves, double temper, double bondAffinity)
        {
            Courage = courage;
            Nerves = nerves;
            Temper = temper;
            BondAffinity = bondAffinity;
        }

        /// <summary>Sheet column "Break difficulty": Temper minus Bond affinity. Only used for wild horses.</summary>
        public double BreakDifficulty => Temper - BondAffinity;

        public static bool InRange(double v) => v >= Min && v <= Max;
    }
}
