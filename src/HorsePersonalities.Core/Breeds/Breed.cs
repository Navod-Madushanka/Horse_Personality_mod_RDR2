using System.Collections.Generic;

namespace HorsePersonalities.Core.Breeds
{
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

        public Breed(string name, IReadOnlyList<string> classes, StatTier tier, TraitLeans leans,
            IReadOnlyList<Quirk> likelyQuirks, string demeanor, IReadOnlyList<string> models,
            LearningRate learning, bool learningOverridden)
        {
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
