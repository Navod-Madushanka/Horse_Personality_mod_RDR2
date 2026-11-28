using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>
    /// Signature behaviors (Build plan: Phase 7b, the sheet's Signatures tab). Habits a coat's written personality calls
    /// for, built from what the game allows. Cut ideas are not listed here.
    /// </summary>
    public enum Signature
    {
        SnakeDrama,
        MoodyRefusal,
        MudTantrum,
        TownDisdain,
        CityLover,
        NightOwl,
        DramaticRear,
        EscapeArtist,
        Prankster,
        LazyPace,
        RoadsideGrazer,
        HillSigh,
        BrushExcitement,
        Scatterbrain,
        WolfBrawler,
        SoulStare,
        CampCreeper,
        // Missouri Fox Trotter coats (Nov 27, 2026)
        Stoic,
        Wanderlust,
        BoredBuck,
        Clumsy,
        SnackMagnet,
        // Mustang coats (Nov 27, 2026)
        ShortKing,
        SelectiveHearing,
        SaddleHater,
        BountyStomper,
        GuardDog,
        SilentStealth,
        Unhurried,
        Jealous,
        // Turkoman coats (Nov 27, 2026)
        Restless,
        TrailSnob,
        WarHorse,
        Snob,
        Showboat,
        MustLead,
        Vain,
        // American Standardbred coats (Nov 27, 2026)
        Racer,
        Spoiled,
        SeparationAnxiety,
        NoBrakes,
        // Nokota coats (Nov 27, 2026)
        Grudge,
        DownhillSnob,
        StrangerDanger,
        // Thoroughbred coats (Nov 27, 2026)
        NeedsSoothing,
        HillCrawler,
        WaterSigh,
        AppleBeggar,
        // Andalusian coats (Nov 28, 2026)
        HatesBrushing,
        // Ardennes coats (Nov 28, 2026)
        Balker,
        // Hungarian Halfbred coats (Nov 28, 2026)
        Strict,
    }

    /// <summary>
    /// A coat's hand-written personality (Build plan: Coat personalities; the sheet's Coats tab). Its leanings replace the
    /// breed's, its signature quirks and behaviors always show, and individuals only stray from it by Coat spread.
    /// </summary>
    public sealed class Coat
    {
        public string Model { get; }
        public string Breed { get; }
        public string Archetype { get; }
        public string Vibe { get; }
        public TraitLeans Leans { get; }
        public IReadOnlyList<Quirk> SignatureQuirks { get; }
        public IReadOnlyList<Signature> Signatures { get; }
        public IReadOnlyList<string> LovesTreats { get; }
        public IReadOnlyList<string> IgnoresTreats { get; }
        public DemeanorTraits Demeanor { get; }

        public Coat(string model, string breed, string archetype, string vibe, TraitLeans leans, IReadOnlyList<Quirk> signatureQuirks,
            IReadOnlyList<Signature> signatures, IReadOnlyList<string> lovesTreats, IReadOnlyList<string> ignoresTreats, DemeanorTraits demeanor)
        {
            Model = model; Breed = breed; Archetype = archetype; Vibe = vibe; Leans = leans; SignatureQuirks = signatureQuirks;
            Signatures = signatures; LovesTreats = lovesTreats; IgnoresTreats = ignoresTreats; Demeanor = demeanor;
        }

        public bool Has(Signature s) => ((IList<Signature>)Signatures).Contains(s);
    }

    public static class SignatureNames
    {
        public static bool TryParse(string text, out Signature signature)
        {
            string n = NameMatching.Normalize(text);
            foreach (Signature s in Enum.GetValues(typeof(Signature)))
            {
                if (NameMatching.Normalize(s.ToString()) == n)
                {
                    signature = s;
                    return true;
                }
            }
            signature = default;
            return false;
        }

        /// <summary>"TownDisdain" as the sheet writes it: "Town disdain".</summary>
        public static string Label(Signature s)
        {
            string name = s.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) sb.Append(' ').Append(char.ToLowerInvariant(name[i]));
                else sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }
}
