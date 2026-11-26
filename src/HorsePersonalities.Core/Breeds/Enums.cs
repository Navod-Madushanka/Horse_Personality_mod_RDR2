using System;
using System.Text;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>The 7 quirks from the build plan. A horse gets 1 or 2. Show-off was cut on Nov 25, 2026 (no scriptable piaffe).</summary>
    public enum Quirk
    {
        Chatterbox,
        Kicker,
        HeadTosser,
        Nosy,
        Shadow,
        Wanderer,
        PickyEater,
    }

    /// <summary>
    /// A breed's stat tier, which is its Handling class on the sheet (Build plan: Hard rules). Natural stats and
    /// skills never push a horse past its tier's caps. Listed from lowest ceiling to highest; do not reorder.
    /// </summary>
    public enum StatTier
    {
        Heavy = 0,
        Standard = 1,
        Race = 2,
        Elite = 3,
    }

    /// <summary>
    /// Why a model is on the sheet's Prized tab (Build plan: Prized horses). Stable-bought horses are prized by their
    /// origin instead, so they need no entry.
    /// </summary>
    public enum PrizedReason
    {
        Special,
        Rare,
        CommunityFavorite,
    }

    public static class NameMatching
    {
        /// <summary>
        /// Lowercase letters and digits only, so the sheet's "Head-tosser" and "Picky eater" match the enum's
        /// HeadTosser and PickyEater.
        /// </summary>
        public static string Normalize(string s)
        {
            if (s == null) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        public static bool TryParseTier(string text, out StatTier tier)
        {
            string n = Normalize(text);
            foreach (StatTier t in Enum.GetValues(typeof(StatTier)))
            {
                if (Normalize(t.ToString()) == n)
                {
                    tier = t;
                    return true;
                }
            }
            tier = default;
            return false;
        }

        public static bool TryParsePrizedReason(string text, out PrizedReason reason)
        {
            string n = Normalize(text);
            foreach (PrizedReason r in Enum.GetValues(typeof(PrizedReason)))
            {
                if (Normalize(r.ToString()) == n)
                {
                    reason = r;
                    return true;
                }
            }
            reason = default;
            return false;
        }

        public static bool TryParseQuirk(string text, out Quirk quirk)
        {
            string n = Normalize(text);
            foreach (Quirk q in Enum.GetValues(typeof(Quirk)))
            {
                if (Normalize(q.ToString()) == n)
                {
                    quirk = q;
                    return true;
                }
            }
            quirk = default;
            return false;
        }
    }
}
