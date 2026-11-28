using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;

namespace HorsePersonalities.Core.Tracking
{
    /// <summary>What Phase 0 shows on mount: model name, breed and bond level.</summary>
    public struct HorseDescription
    {
        public uint ModelHash;
        public string ModelName;
        /// <summary>Null when the model is not tied to a breed (gang horses, mules, Online-only breeds, unknown).</summary>
        public Breed Breed;
        public string Note;
        public bool IsMale;
        /// <summary>GET_ATTRIBUTE_RANK for PA_BONDING. Phase 1 confirms whether this matches the game's 1-4 bond level.</summary>
        public int BondRank;
        public int BondPoints;
        /// <summary>
        /// The mod's trust level (1-4) for a permanent horse with trust (Build plan: Trust); 0 when there is none (a
        /// temporary personality, or before the first read). Set by the caller, which knows the horse's record.
        /// </summary>
        public int TrustLevel;

        /// <summary>The level the mod's features use: trust when the horse has it, otherwise the game's bond.</summary>
        public int FeatureBond => TrustLevel > 0 ? TrustLevel : BondRank;

        public string BreedLabel => Breed != null ? Breed.Name : (string.IsNullOrEmpty(Note) ? "Unknown breed" : "No breed (" + Note + ")");

        public string ToSubtitle() => $"{BreedLabel} - {ModelName} - {(IsMale ? "male" : "female")} - bond {BondRank}";
    }

    public static class HorseDescriber
    {
        public static HorseDescription Describe(IGameApi game, BreedCatalog catalog, int horse)
        {
            uint model = game.GetModel(horse);
            var d = new HorseDescription
            {
                ModelHash = model,
                ModelName = catalog.DescribeModel(model),
                IsMale = game.IsMale(horse),
            };
            if (catalog.TryGetBreedByModel(model, out Breed breed)) d.Breed = breed;
            else if (catalog.TryGetOtherModel(model, out OtherModel other)) d.Note = other.Note;
            AttributeReading bond = game.ReadAttribute(horse, PedAttribute.Bonding);
            d.BondRank = bond.Rank;
            d.BondPoints = bond.Points;
            return d;
        }
    }
}
