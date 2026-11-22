namespace HorsePersonalities.Core.Hashing
{
    /// <summary>
    /// Jenkins one-at-a-time hash, as used by the game's GET_HASH_KEY (0xFD340785ADF8CFB7).
    /// The game lowercases ASCII before hashing, so "A_C_Horse_Arabian_White" and "a_c_horse_arabian_white" match.
    /// Lets Core turn model names from breeds.json into the hashes GET_ENTITY_MODEL returns, without the game.
    /// </summary>
    public static class Joaat
    {
        public static uint Hash(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            unchecked
            {
                uint h = 0;
                foreach (char ch in text)
                {
                    char c = ch >= 'A' && ch <= 'Z' ? (char)(ch + 32) : ch;
                    h += (byte)c;
                    h += h << 10;
                    h ^= h >> 6;
                }
                h += h << 3;
                h ^= h >> 11;
                h += h << 15;
                return h;
            }
        }
    }
}
