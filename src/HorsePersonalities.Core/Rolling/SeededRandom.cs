using System;

namespace HorsePersonalities.Core.Rolling
{
    /// <summary>
    /// SplitMix64: a small, fast generator whose output depends only on the seed, on every .NET version.
    /// System.Random is not used because its sequence for a seed is not guaranteed across runtimes, and a
    /// horse's seed must give the same horse forever.
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong NextULong()
        {
            unchecked
            {
                ulong z = _state += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>
        /// Standard normal (mean 0, deviation 1) by Box-Muller. Always uses exactly two draws, so the number of
        /// draws in a roll never depends on the values rolled.
        /// </summary>
        public double NextGaussian()
        {
            double u1 = 1.0 - NextDouble(); // (0, 1], so the log is finite
            double u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>A fresh seed for a horse rolled for the first time.</summary>
        public static ulong NewSeed()
        {
            byte[] b = Guid.NewGuid().ToByteArray();
            return BitConverter.ToUInt64(b, 0) ^ BitConverter.ToUInt64(b, 8);
        }
    }
}
