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
    /// Natural stat differences in whole ranks above or below the breed baseline (+1 = one more stat-bar segment).
    /// They set the horse's ceiling; Phase 5 reveals health and stamina by bond level, Phase 8 grows speed and
    /// acceleration toward theirs.
    /// </summary>
    public sealed class NaturalStats
    {
        public int Speed { get; }
        public int Acceleration { get; }
        public int Health { get; }
        public int Stamina { get; }

        public NaturalStats(int speed, int acceleration, int health, int stamina)
        {
            Speed = speed;
            Acceleration = acceleration;
            Health = health;
            Stamina = stamina;
        }

        public IEnumerable<(string Name, int Value)> All()
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
        public const int RollerVersion = 4; // 2: ranks, Show-off cut (Nov 25). 3: prized (Nov 26). 4: coat personalities (Nov 27).

        public string Breed { get; }
        public HorseOrigin Origin { get; }
        public ulong Seed { get; }
        public HorseQuality Quality { get; }
        public Traits Traits { get; }
        public IReadOnlyList<Quirk> Quirks { get; }
        public NaturalStats NaturalStats { get; }
        public BreakingDifficulty Breaking { get; }
        /// <summary>Rolled as a prized horse (Build plan: Prized horses): always good, no trait flaws, slower to bond 4.</summary>
        public bool Prized { get; }
        /// <summary>The coat's archetype ("The Ice Queen") when the horse was rolled from a coat design; null otherwise.</summary>
        public string Archetype { get; }
        /// <summary>The coat's signature behaviors (Phase 7b). Empty without a coat design.</summary>
        public IReadOnlyList<Signature> Signatures { get; }

        public Personality(string breed, HorseOrigin origin, ulong seed, HorseQuality quality, Traits traits,
            IReadOnlyList<Quirk> quirks, NaturalStats naturalStats, BreakingDifficulty breaking, bool prized = false,
            string archetype = null, IReadOnlyList<Signature> signatures = null)
        {
            Prized = prized;
            Archetype = archetype;
            Signatures = signatures ?? Array.Empty<Signature>();
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
            string R(int v) => v.ToString("+0;-0;0", CultureInfo.InvariantCulture);
            var lines = new List<string>
            {
                $"{Breed} - {Quality.ToString().ToLowerInvariant()} horse{(Prized ? " (prized)" : "")} - seed {Seed:X16}",
                $"Courage {T(Traits.Courage)}  Nerves {T(Traits.Nerves)}  Temper {T(Traits.Temper)}  Bond {T(Traits.BondAffinity)}",
                "Quirks: " + string.Join(", ", Quirks.Select(q => q.ToString())),
                $"Natural stats (ranks): speed {R(NaturalStats.Speed)}, accel {R(NaturalStats.Acceleration)}, health {R(NaturalStats.Health)}, stamina {R(NaturalStats.Stamina)}",
                Origin == HorseOrigin.Wild ? $"Breaking: {Breaking}" : $"Breaking: {Breaking} ({Origin.ToString().ToLowerInvariant()} horse)",
            };
            if (Archetype != null)
                lines.Insert(1, $"Coat: {Archetype}" + (Signatures.Count > 0 ? " - " + string.Join(", ", Signatures.Select(SignatureNames.Label)) : ""));
            return lines;
        }

        public bool Has(Signature s) => ((IList<Signature>)Signatures).Contains(s);

        public override string ToString() => string.Join(" | ", Describe());
    }
}
