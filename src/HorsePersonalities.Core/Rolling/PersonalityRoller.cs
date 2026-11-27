using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Settings;

namespace HorsePersonalities.Core.Rolling
{
    /// <summary>
    /// Rolls a horse's hidden personality from its breed and a seed (Build plan: How traits are rolled).
    /// Pure: the same breed, origin, seed and settings always give the same personality.
    ///
    /// 1. Good or bad: GoodHorseChance.
    /// 2. Traits: each trait strays from the breed's leaning by QualityShift toward good (good horse) or bad
    ///    (bad horse), plus a normal spread of TraitSpread. A good horse keeps at most one trait more than
    ///    FlawMargin on the bad side of its breed; any others are mirrored to the good side. A bad horse the reverse.
    /// 3. Quirks: 1, or 2 with SecondQuirkChance; the breed's likely quirks weigh LikelyQuirkWeight, others 1.
    /// 4. Natural stats: whole ranks. Each stat leans StatQualityShift ranks toward the horse's quality with a normal
    ///    spread of StatSpread ranks, is rounded to a whole rank, and is held inside the tier caps and MaxStatRankChange.
    /// 5. Breaking (wild horses only): Temper minus Bond affinity against the Hard and Unbreakable scores, and
    ///    Unbreakable also needs an UnbreakableChance roll.
    ///
    /// Prized horses (Build plan: Prized horses): always good, every trait on the good side of the breed (no flaw at
    /// all), and a wild one adds PrizedBreakScoreBonus to its breaking score but is never Unbreakable.
    ///
    /// Every roll makes the same number of draws whatever it rolls, so changing one setting never reshuffles
    /// the parts of a horse it doesn't touch.
    /// </summary>
    public sealed class PersonalityRoller
    {
        private static readonly Quirk[] AllQuirks = (Quirk[])Enum.GetValues(typeof(Quirk));

        private readonly RollerTuning _tuning;
        private readonly BreedCatalog _catalog;
        private readonly IReadOnlyDictionary<StatTier, TierCap> _caps;
        private readonly double _goodChance;
        private readonly double _unbreakableChance;
        private readonly int _maxRankChange;

        public PersonalityRoller(BreedCatalog catalog, ModSettings settings)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _tuning = catalog.Roller;
            _catalog = catalog;
            _caps = catalog.TierCaps;
            _goodChance = settings.GoodHorseChance / 100.0;
            _unbreakableChance = settings.UnbreakableChance / 100.0;
            _maxRankChange = settings.MaxStatRankChange;
        }

        /// <summary>Most ranks a natural stat may rise and drop for this tier: the tier cap, never past MaxStatRankChange.</summary>
        public (int Up, int Down) StatLimits(StatTier tier)
        {
            TierCap cap = _caps[tier];
            return (Math.Min(cap.Up, _maxRankChange), Math.Min(cap.Down, _maxRankChange));
        }

        /// <param name="model">The horse's model. With a coat design (Phase 7b) its leanings, signature quirks and signatures are used.</param>
        /// <param name="exactDesign">Test mode: a coat's design with no dice on the traits (Coat design preset).</param>
        public Personality Roll(Breed breed, HorseOrigin origin, ulong seed, bool prized = false, string model = null, bool exactDesign = false)
        {
            if (breed == null) throw new ArgumentNullException(nameof(breed));
            Coat coat = null;
            if (model != null) _catalog.TryGetCoat(model, out coat);
            TraitLeans leans = coat?.Leans ?? breed.Leans;
            double spread = coat == null ? _tuning.TraitSpread : exactDesign ? 0 : _tuning.CoatSpread;
            var rng = new SeededRandom(seed);

            // 1. Good or bad.
            bool good = rng.NextDouble() < _goodChance || prized; // the draw is made either way: fixed draw count
            int q = good ? 1 : -1;

            // 2. Traits. d = how far each trait sits on the good side of the breed (negative = bad side).
            var d = new double[TraitInfo.All.Length];
            for (int i = 0; i < d.Length; i++)
                d[i] = (exactDesign && coat != null ? 0 : q * _tuning.QualityShift * (coat != null ? 0.5 : 1)) + spread * rng.NextGaussian();
            if (prized) NoFlaws(d);
            else LimitWrongSide(d, q, _tuning.FlawMargin);

            var values = new double[d.Length];
            for (int i = 0; i < d.Length; i++)
            {
                Trait t = TraitInfo.All[i];
                values[i] = Clamp(TraitInfo.Lean(leans, t) + TraitInfo.GoodDirection(t) * d[i], TraitLeans.Min, TraitLeans.Max);
            }
            var traits = new Traits(values[0], values[1], values[2], values[3]);

            // 3. Quirks.
            bool two = rng.NextDouble() < _tuning.SecondQuirkChance;
            var quirks = new List<Quirk>(2);
            Quirk first = PickQuirk(rng, breed, quirks);
            Quirk second = PickQuirk(rng, breed, new List<Quirk> { first }); // always drawn: fixed draw count
            if (coat != null && coat.SignatureQuirks.Count > 0)
            {
                // A coat's signature quirks always show; a single one may get a rolled companion.
                quirks.AddRange(coat.SignatureQuirks);
                if (quirks.Count == 1 && two && !exactDesign)
                    quirks.Add(quirks.Contains(first) ? second : first);
            }
            else
            {
                quirks.Add(first);
                if (two) quirks.Add(second);
            }

            // 4. Natural stats.
            var (up, down) = StatLimits(breed.Tier);
            int Stat()
            {
                double ranks = q * _tuning.StatQualityShift + _tuning.StatSpread * rng.NextGaussian();
                int whole = (int)Math.Round(ranks, MidpointRounding.AwayFromZero);
                return whole > up ? up : whole < -down ? -down : whole;
            }
            var stats = new NaturalStats(Stat(), Stat(), Stat(), Stat());

            // 5. Breaking.
            double breakRoll = rng.NextDouble();
            var breaking = BreakingDifficulty.Normal;
            if (origin == HorseOrigin.Wild)
            {
                double score = traits.Temper - traits.BondAffinity + (prized ? _tuning.PrizedBreakScoreBonus : 0);
                if (!prized && score >= _tuning.UnbreakableBreakScore && breakRoll < _unbreakableChance) breaking = BreakingDifficulty.Unbreakable;
                else if (score >= _tuning.HardBreakScore) breaking = BreakingDifficulty.Hard;
            }

            return new Personality(breed.Name, origin, seed, good ? HorseQuality.Good : HorseQuality.Bad, traits, quirks, stats, breaking, prized,
                coat?.Archetype, coat?.Signatures);
        }

        /// <summary>
        /// Keeps at most one trait past the margin on the wrong side for this quality (bad side for a good horse,
        /// good side for a bad horse): the most extreme one stays, the rest are mirrored to the right side.
        /// </summary>
        internal static void LimitWrongSide(double[] d, int q, double margin)
        {
            int keep = -1;
            for (int i = 0; i < d.Length; i++)
                if (q * d[i] < -margin && (keep < 0 || Math.Abs(d[i]) > Math.Abs(d[keep]))) keep = i;
            for (int i = 0; i < d.Length; i++)
                if (i != keep && q * d[i] < -margin) d[i] = -d[i];
        }

        /// <summary>A prized horse has no trait on the bad side of its breed: every bad-side trait is mirrored to the good side.</summary>
        internal static void NoFlaws(double[] d)
        {
            for (int i = 0; i < d.Length; i++)
                if (d[i] < 0) d[i] = -d[i];
        }

        private Quirk PickQuirk(SeededRandom rng, Breed breed, List<Quirk> exclude)
        {
            double total = 0;
            foreach (Quirk k in AllQuirks)
                if (!exclude.Contains(k)) total += Weight(breed, k);
            double r = rng.NextDouble() * total;
            Quirk last = AllQuirks[0];
            foreach (Quirk k in AllQuirks)
            {
                if (exclude.Contains(k)) continue;
                last = k;
                r -= Weight(breed, k);
                if (r < 0) return k;
            }
            return last; // only reached by rounding at the very top of the range
        }

        private double Weight(Breed breed, Quirk k) => breed.LikelyQuirks.Contains(k) ? _tuning.LikelyQuirkWeight : 1.0;

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
