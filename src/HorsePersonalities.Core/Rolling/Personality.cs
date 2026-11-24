using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HorsePersonalities.Core.Breeds;

namespace HorsePersonalities.Core.Rolling
{
    public enum HorseQuality { Good, Bad }

    /// <summary>Where the horse came from. Only wild horses roll a breaking difficulty.</summary>
    public enum HorseOrigin { Stable, Wild, Unknown }

    public enum BreakingDifficulty { Normal, Hard, Unbreakable }

    public enum Trait { Courage, Nerves, Temper, BondAffinity }

    public static class TraitInfo
    {
        public static readonly Trait[] All = { Trait.Courage, Trait.Nerves, Trait.Temper, Trait.BondAffinity };

        /// <summary>
        /// +1 when a higher value is better for the rider (braver, bonds faster), -1 when lower is better
        /// (calmer nerves, longer fuse). Fixed by the trait's meaning, not a tuning number.
        /// </summary>
        public static int GoodDirection(Trait t) => t == Trait.Courage || t == Trait.BondAffinity ? 1 : -1;

        public static double Lean(TraitLeans leans, Trait t)
        {
            switch (t)
            {
                case Trait.Courage: return leans.Courage;
                case Trait.Nerves: return leans.Nerves;
                case Trait.Temper: return leans.Temper;
                default: return leans.BondAffinity;
            }
        }
    }

    /// <summary>A horse's four traits on the sheet's -2..+2 scale.</summary>
    public sealed class Traits
    {
        public double Courage { get; }
        public double Nerves { get; }
        public double Temper { get; }
        public double BondAffinity { get; }

        public Traits(double courage, double nerves, double temper, double bondAffinity)
        {
            Courage = courage;
            Nerves = nerves;
            Temper = temper;
            BondAffinity = bondAffinity;
        }

        public double this[Trait t]
        {
            get
            {
                switch (t)
                {
                    case Trait.Courage: return Courage;
                    case Trait.Nerves: return Nerves;
                    case Trait.Temper: return Temper;
                    default: return BondAffinity;
                }
            }
        }
    }

    /// <summary>
    /// Natural stat differences as fractions of the breed baseline (0.04 = 4% above). They set the horse's ceiling;
    /// Phase 5 reveals health and stamina by bond level, Phase 8 grows speed and acceleration toward theirs.
    /// </summary>
    public sealed class NaturalStats
    {
        public double Speed { get; }
        public double Acceleration { get; }
        public double Health { get; }
        public double Stamina { get; }

        public NaturalStats(double speed, double acceleration, double health, double stamina)
        {
            Speed = speed;
            Acceleration = acceleration;
            Health = health;
            Stamina = stamina;
        }

        public IEnumerable<(string Name, double Value)> All()
        {
            yield return ("speed", Speed);
            yield return ("acceleration", Acceleration);
            yield return ("health", Health);
            yield return ("stamina", Stamina);
        }
    }

    /// <summary>One horse's rolled personality (Build plan: layer 2, Individual). Hidden from the player.</summary>
    public sealed class Personality
    {
        /// <summary>Bumped when the roll algorithm changes, so a saved seed can be told apart from a new one.</summary>
        public const int RollerVersion = 1;

        public string Breed { get; }
        public HorseOrigin Origin { get; }
        public ulong Seed { get; }
        public HorseQuality Quality { get; }
        public Traits Traits { get; }
        public IReadOnlyList<Quirk> Quirks { get; }
        public NaturalStats NaturalStats { get; }
        public BreakingDifficulty Breaking { get; }

        public Personality(string breed, HorseOrigin origin, ulong seed, HorseQuality quality, Traits traits,
            IReadOnlyList<Quirk> quirks, NaturalStats naturalStats, BreakingDifficulty breaking)
        {
            Breed = breed;
            Origin = origin;
            Seed = seed;
            Quality = quality;
            Traits = traits;
            Quirks = quirks;
            NaturalStats = naturalStats;
            Breaking = breaking;
        }

        /// <summary>Lines for the debug overlay and the log.</summary>
        public IReadOnlyList<string> Describe()
        {
            string T(double v) => v.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
            string P(double v) => (v * 100).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%";
            return new[]
            {
                $"{Breed} - {Quality.ToString().ToLowerInvariant()} horse - seed {Seed:X16}",
                $"Courage {T(Traits.Courage)}  Nerves {T(Traits.Nerves)}  Temper {T(Traits.Temper)}  Bond {T(Traits.BondAffinity)}",
                "Quirks: " + string.Join(", ", Quirks.Select(q => q.ToString())),
                $"Natural stats: speed {P(NaturalStats.Speed)}, accel {P(NaturalStats.Acceleration)}, health {P(NaturalStats.Health)}, stamina {P(NaturalStats.Stamina)}",
                Origin == HorseOrigin.Wild ? $"Breaking: {Breaking}" : $"Breaking: {Breaking} ({Origin.ToString().ToLowerInvariant()} horse)",
            };
        }

        public override string ToString() => string.Join(" | ", Describe());
    }
}
