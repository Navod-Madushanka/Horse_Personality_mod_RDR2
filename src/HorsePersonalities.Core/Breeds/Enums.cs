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
