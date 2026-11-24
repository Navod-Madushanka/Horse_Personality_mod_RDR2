using System;

namespace HorsePersonalities.Core.Identity
{
    /// <summary>
    /// How the mod tells horses apart across reloads (Build plan: horses.json key). The game has no permanent
    /// horse id a script can read, so a horse is its model, sex and coat. The coat is "default" until the game
    /// shows a way to read a horse's current outfit (Phase 2 checks with a DevTools readout).
    /// Two owned horses with the same fingerprint share a personality; HorseRegistry warns when that happens.
    /// </summary>
    public sealed class HorseFingerprint : IEquatable<HorseFingerprint>
    {
        public const string DefaultCoat = "default";

        public string Model { get; }
        public bool IsMale { get; }
        public string Coat { get; }

        public HorseFingerprint(string model, bool isMale, string coat = DefaultCoat)
        {
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("A fingerprint needs a model.", nameof(model));
            Model = model.Trim().ToLowerInvariant();
            IsMale = isMale;
            Coat = string.IsNullOrWhiteSpace(coat) ? DefaultCoat : coat.Trim().ToLowerInvariant();
            if (Model.Contains("|") || Coat.Contains("|")) throw new ArgumentException("Fingerprint parts cannot contain '|'.");
        }

        /// <summary>The horses.json key, e.g. "a_c_horse_turkoman_gold|male|default".</summary>
        public string Key => $"{Model}|{(IsMale ? "male" : "female")}|{Coat}";

        public static bool TryParse(string key, out HorseFingerprint fingerprint)
        {
            fingerprint = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            string[] parts = key.Split('|');
            if (parts.Length != 3 || parts[0].Trim().Length == 0) return false;
            bool male;
            if (parts[1] == "male") male = true;
            else if (parts[1] == "female") male = false;
            else return false;
            fingerprint = new HorseFingerprint(parts[0], male, parts[2]);
            return true;
        }

        public bool Equals(HorseFingerprint other) => other != null && Key == other.Key;
        public override bool Equals(object obj) => Equals(obj as HorseFingerprint);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);
        public override string ToString() => Key;
    }
}
