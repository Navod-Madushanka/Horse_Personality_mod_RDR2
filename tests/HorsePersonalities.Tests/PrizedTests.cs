using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Saving;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Prized horses (decided Nov 25, 2026).</summary>
    public class PrizedTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly PersonalityRoller Roller = new PersonalityRoller(Catalog, ModSettings.Defaults());

        private static Breed B(string n) { Catalog.TryGetBreed(n, out Breed b); return b; }

        private static List<Personality> Rolls(string breed, HorseOrigin origin, bool prized, int n = 10_000) =>
            Enumerable.Range(0, n).Select(i => Roller.Roll(B(breed), origin, (ulong)i * 7 + 3, prized)).ToList();

        [Theory]
        [InlineData("Mustang")]
        [InlineData("Thoroughbred")]
        [InlineData("Arabian")]
        [InlineData("Shire")]
        public void PrizedHorsesAreAlwaysGoodWithNoTraitOnTheBadSide(string breed)
        {
            Breed b = B(breed);
            foreach (var p in Rolls(breed, HorseOrigin.Stable, prized: true))
            {
                Assert.Equal(HorseQuality.Good, p.Quality);
                Assert.True(p.Prized);
                foreach (Trait t in TraitInfo.All)
                    Assert.True(TraitInfo.GoodDirection(t) * (p.Traits[t] - TraitInfo.Lean(b.Leans, t)) >= -1e-9,
                        $"{breed} seed {p.Seed}: {t} {p.Traits[t]:0.00} is on the bad side of its leaning");
            }
        }

        [Fact]
        public void PrizedHorsesStillHaveQuirks()
        {
            Assert.All(Rolls("Turkoman", HorseOrigin.Stable, true, 2000), p => Assert.InRange(p.Quirks.Count, 1, 2));
        }

        [Fact]
        public void PrizedWildHorsesBreakHardMoreOftenButNeverUnbreakable()
        {
            var roller = new PersonalityRoller(Catalog, ModSettings.Load("UnbreakableChance = 100\n", new MemoryLog()));
            List<Personality> R(bool prized) => Enumerable.Range(0, 10_000).Select(i => roller.Roll(B("Mustang"), HorseOrigin.Wild, (ulong)i, prized)).ToList();
            var normal = R(false);
            var prizedRolls = R(true);
            Assert.Contains(normal, p => p.Breaking == BreakingDifficulty.Unbreakable);
            Assert.DoesNotContain(prizedRolls, p => p.Breaking == BreakingDifficulty.Unbreakable);
            // Prized horses are calmer and bond better, which lowers the score; the bonus must more than make up for it
            // against an equally good, non-prized horse.
            double hardPrized = prizedRolls.Count(p => p.Breaking == BreakingDifficulty.Hard) / 10_000.0;
            Assert.True(hardPrized > 0.05, $"prized Mustang Hard share {hardPrized}");
            foreach (var p in prizedRolls)
            {
                double score = p.Traits.Temper - p.Traits.BondAffinity + Catalog.Roller.PrizedBreakScoreBonus;
                Assert.Equal(score >= Catalog.Roller.HardBreakScore, p.Breaking == BreakingDifficulty.Hard);
            }
        }

        [Fact]
        public void SameSeedPrizedAndNotShareQuirksAndStatDraws()
        {
            // Prized changes quality and flaws only; the quirk draws stay where they were.
            var a = Roller.Roll(B("Morgan"), HorseOrigin.Stable, 99, prized: false);
            var b = Roller.Roll(B("Morgan"), HorseOrigin.Stable, 99, prized: true);
            if (a.Quality == HorseQuality.Good) Assert.Equal(a.Quirks, b.Quirks);
            Assert.Equal(a.Seed, b.Seed);
        }

        [Fact]
        public void RegistryRollsStableHorsesAsPrized()
        {
            ulong seed = 1;
            var reg = new HorseRegistry(Roller, new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 26));
            var bought = reg.OnSaddle(5, new HorseFingerprint("a_c_horse_morgan_bay", true), B("Morgan"));
            Assert.True(bought.Personality.Prized);
            var stolen = reg.OnMount(6, new HorseFingerprint("a_c_horse_morgan_bayroan", true), B("Morgan"), HorseOrigin.Unknown, false);
            Assert.False(stolen.Personality.Prized);
            Assert.False(reg.OnSaddle(6, stolen.Id, B("Morgan")).Personality.Prized); // a saddled stolen horse keeps its roll
        }

        [Fact]
        public void RegistryRollsPrizedModelsAsPrizedHoweverFound()
        {
            ulong seed = 1;
            var reg = new HorseRegistry(Roller, new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 26),
                id => Catalog.TryGetPrized(id.Model, out _));
            var wild = reg.OnMount(7, new HorseFingerprint("a_c_horse_arabian_redchestnut_pc", false), B("Arabian"), HorseOrigin.Wild, false);
            Assert.True(wild.Personality.Prized);
            Assert.NotEqual(BreakingDifficulty.Unbreakable, wild.Personality.Breaking);
            var kept = reg.OnSaddle(7, wild.Id, B("Arabian"));
            Assert.True(kept.Personality.Prized);
            Assert.Equal(HorseOrigin.Wild, kept.Personality.Origin);
        }

        [Fact]
        public void PrizedIsSavedAndLoaded()
        {
            ulong seed = 1;
            var reg = new HorseRegistry(Roller, new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 26));
            reg.OnSaddle(5, new HorseFingerprint("a_c_horse_morgan_bay", true), B("Morgan"));
            var back = HorseSaveFormat.Read(HorseSaveFormat.Write(reg.Permanent), new MemoryLog()).Single();
            Assert.True(back.Personality.Prized);
            Assert.Contains("(prized)", back.Personality.Describe()[0]);
        }

        [Fact]
        public void FileFromBeforePrizedReadsAsNotPrized()
        {
            var records = HorseSaveFormat.Read(TestData.Read("tests", "HorsePersonalities.Tests", "Fixtures", "horses-schema1.json"), new MemoryLog());
            Assert.All(records, r => Assert.False(r.Personality.Prized));
        }
    }
}
