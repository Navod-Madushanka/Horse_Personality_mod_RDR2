using System.Collections.Generic;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>A breed's default body language (sheet's Demeanor tab), each 0 to 2.</summary>
    public sealed class DemeanorTraits
    {
        public double Impatient { get; }
        public double Alert { get; }
        public double Affectionate { get; }
        public double Fussy { get; }

        public DemeanorTraits(double impatient, double alert, double affectionate, double fussy)
        {
            Impatient = impatient;
            Alert = alert;
            Affectionate = affectionate;
            Fussy = fussy;
        }

        public static readonly DemeanorTraits None = new DemeanorTraits(0, 0, 0, 0);
        public const double Max = 2.0;
    }

    /// <summary>One row of the breed sheet, plus the game models that belong to it.</summary>
    public sealed class Breed
    {
        public string Name { get; }
        public IReadOnlyList<string> Classes { get; }
        /// <summary>The game's handling class, which is also the breed's stat tier.</summary>
        public StatTier Tier { get; }
        public TraitLeans Leans { get; }
        public IReadOnlyList<Quirk> LikelyQuirks { get; }
        public string Demeanor { get; }
        public IReadOnlyList<string> Models { get; }
        public LearningRate Learning { get; }
        /// <summary>True when the sheet typed over the class formula for this breed.</summary>
        public bool LearningOverridden { get; }
        public DemeanorTraits DemeanorTraits { get; }

        public Breed(string name, IReadOnlyList<string> classes, StatTier tier, TraitLeans leans,
            IReadOnlyList<Quirk> likelyQuirks, string demeanor, IReadOnlyList<string> models,
            LearningRate learning, bool learningOverridden, DemeanorTraits demeanorTraits = null)
        {
            DemeanorTraits = demeanorTraits ?? DemeanorTraits.None;
            Name = name;
            Classes = classes;
            Tier = tier;
            Leans = leans;
            LikelyQuirks = likelyQuirks;
            Demeanor = demeanor;
            Models = models;
            Learning = learning;
            LearningOverridden = learningOverridden;
        }

        public bool IsMultiClass => Classes.Count > 1;

        public override string ToString() => Name;
    }
}
