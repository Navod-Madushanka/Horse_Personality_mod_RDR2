using System;
using System.Text;

namespace HorsePersonalities.Core.Breeds
{
    /// <summary>The 8 quirks from the build plan. A horse gets 1 or 2.</summary>
    public enum Quirk
    {
        Chatterbox,
        Kicker,
        HeadTosser,
        Nosy,
        Shadow,
        Wanderer,
        PickyEater,
        ShowOff,
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
