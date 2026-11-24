using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 3 tests: 10,000 rolls per breed.</summary>
    public class RollerTests
    {
        private const int Rolls = 10_000;
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly PersonalityRoller Default = new PersonalityRoller(Catalog, ModSettings.Defaults());

        private static ModSettings With(string ini) => ModSettings.Load("[HorsePersonalities]\n" + ini + "\n", new MemoryLog());

        private static Breed B(string name)
        {
            Assert.True(Catalog.TryGetBreed(name, out Breed b), name);
            return b;
        }

        /// <summary>Different seeds per breed, so breeds don't share one sequence of good/bad rolls.</summary>
        private static List<Personality> RollMany(PersonalityRoller roller, Breed b, HorseOrigin origin = HorseOrigin.Wild, int n = Rolls)
        {
            ulong salt = (ulong)Math.Abs(b.Name.GetHashCode()) << 20; // stable within one test run
            var list = new List<Personality>(n);
            for (int i = 0; i < n; i++) list.Add(roller.Roll(b, origin, salt + (ulong)i));
            return list;
        }

        public static IEnumerable<object[]> AllBreeds() => Catalog.Breeds.Select(b => new object[] { b.Name });

        [Fact]
        public void SameSeedGivesTheSameRoll()
        {
            var a = Default.Roll(B("Turkoman"), HorseOrigin.Wild, 42);
            var b = Default.Roll(B("Turkoman"), HorseOrigin.Wild, 42);
            Assert.Equal(a.ToString(), b.ToString());
            Assert.NotEqual(a.ToString(), Default.Roll(B("Turkoman"), HorseOrigin.Wild, 43).ToString());
        }

        [Fact]
        public void FixedSeedsGiveFixedHorses()
        {
            // Pins the algorithm: if this changes, saved seeds would roll different horses. Only update these
            // values on purpose, together with Personality.RollerVersion.
            var p = Default.Roll(B("Turkoman"), HorseOrigin.Wild, 42);
            Assert.Equal(Golden.Turkoman42, p.ToString());
            var q = Default.Roll(B("Shire"), HorseOrigin.Stable, 0xDEADBEEF);
            Assert.Equal(Golden.ShireDeadBeef, q.ToString());
        }

        [Fact]
        public void OriginOnlyChangesBreaking()
        {
            var wild = Default.Roll(B("Mustang"), HorseOrigin.Wild, 7);
            var stable = Default.Roll(B("Mustang"), HorseOrigin.Stable, 7);
            Assert.Equal(wild.Quality, stable.Quality);
            Assert.Equal(wild.Traits.Temper, stable.Traits.Temper);
            Assert.Equal(wild.Quirks, stable.Quirks);
            Assert.Equal(wild.NaturalStats.Speed, stable.NaturalStats.Speed);
            Assert.Equal(BreakingDifficulty.Normal, stable.Breaking);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void GoodShareStaysWithin2PointsOfTheSetting(string breed)
        {
            double share = RollMany(Default, B(breed)).Count(p => p.Quality == HorseQuality.Good) * 100.0 / Rolls;
            Assert.InRange(share, 78, 82);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void GoodHorseChanceSettingIsFollowed(int chance)
        {
            var roller = new PersonalityRoller(Catalog, With($"GoodHorseChance = {chance}"));
            double share = RollMany(roller, B("Morgan")).Count(p => p.Quality == HorseQuality.Good) * 100.0 / Rolls;
            Assert.InRange(share, chance - 2, chance + 2);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void AverageTraitsFollowTheBreedsLeanings(string breed)
        {
            Breed b = B(breed);
            var rolls = RollMany(Default, b);
            foreach (Trait t in TraitInfo.All)
            {
                double mean = rolls.Average(p => p.Traits[t]);
                double lean = TraitInfo.Lean(b.Leans, t);
                // Most horses are good, so the average sits a little on the good side of the leaning, never far.
                Assert.InRange(mean - lean, -0.75, 0.75);
                // A leaning already at the good end of the scale (Shire nerves -2) can't rise past it: the cap at
                // +-2 pulls its average back toward the middle, so only check that it stays close.
                bool atGoodEnd = TraitInfo.GoodDirection(t) * lean >= TraitLeans.Max;
                if (atGoodEnd) Assert.True(Math.Abs(mean - lean) < 0.3, $"{breed} {t}: average {mean:0.00} far from capped leaning {lean}");
                else Assert.True(TraitInfo.GoodDirection(t) * (mean - lean) > -0.05, $"{breed} {t}: average {mean:0.00} is on the bad side of its leaning {lean}");
            }
        }

        [Fact]
        public void BreedsWithHigherLeaningsRollHigherOnAverage()
        {
            var means = Catalog.Breeds.ToDictionary(b => b, b => RollMany(Default, b, n: 3000));
            foreach (Trait t in TraitInfo.All)
                foreach (Breed x in Catalog.Breeds)
                    foreach (Breed y in Catalog.Breeds)
                        if (TraitInfo.Lean(x.Leans, t) >= TraitInfo.Lean(y.Leans, t) + 1)
                            Assert.True(means[x].Average(p => p.Traits[t]) > means[y].Average(p => p.Traits[t]), $"{t}: {x} should average above {y}");
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void TraitsStayOnTheSheetsScale(string breed)
        {
            foreach (var p in RollMany(Default, B(breed)))
                foreach (Trait t in TraitInfo.All)
                    Assert.InRange(p.Traits[t], -2.0, 2.0);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void GoodHorsesHaveAtMostOneFlawAndBadHorsesAtMostOneGoodTrait(string breed)
        {
            Breed b = B(breed);
            double margin = Catalog.Roller.FlawMargin + 1e-9;
            foreach (var p in RollMany(Default, b))
            {
                int q = p.Quality == HorseQuality.Good ? 1 : -1;
                int wrongSide = TraitInfo.All.Count(t => q * TraitInfo.GoodDirection(t) * (p.Traits[t] - TraitInfo.Lean(b.Leans, t)) < -margin);
                Assert.True(wrongSide <= 1, $"{breed} seed {p.Seed}: {wrongSide} traits on the wrong side for a {p.Quality} horse");
            }
        }

        [Fact]
        public void LeaningsShiftTheOddsButNeverLockThem()
        {
            // "A brave Morgan or a jumpy Ardennes is rare but possible."
            double braveMorgan = RollMany(Default, B("Morgan"), n: 20_000).Count(p => p.Traits.Courage >= 1.5) / 20_000.0;
            double braveArdennes = RollMany(Default, B("Ardennes"), n: 20_000).Count(p => p.Traits.Courage >= 1.5) / 20_000.0;
            double jumpyArdennes = RollMany(Default, B("Ardennes"), n: 20_000).Count(p => p.Traits.Nerves >= 1.0) / 20_000.0;
            Assert.InRange(braveMorgan, 0.005, 0.10);
            Assert.True(braveArdennes > 0.5);
            Assert.InRange(jumpyArdennes, 0.0001, 0.02);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void NoStatEverPassesItsTierCap(string breed)
        {
            Breed b = B(breed);
            TierCap cap = Catalog.TierCaps[b.Tier];
            foreach (var p in RollMany(Default, b))
                foreach (var (name, v) in p.NaturalStats.All())
                    Assert.True(v <= cap.Up + 1e-12 && v >= -cap.Down - 1e-12, $"{breed} {name} {v} outside -{cap.Down}..+{cap.Up}");
        }

        [Fact]
        public void MaxStatVarianceCapsEvenAWideTierCap()
        {
            var roller = new PersonalityRoller(Catalog, With("MaxStatVariance = 3"));
            Assert.Equal((0.03, 0.03), roller.StatLimits(StatTier.Elite));
            foreach (var p in RollMany(roller, B("Arabian")))
                foreach (var (_, v) in p.NaturalStats.All())
                    Assert.InRange(v, -0.03 - 1e-12, 0.03 + 1e-12);
        }

        [Fact]
        public void GoodHorsesHaveBetterNaturalStatsOnAverage()
        {
            var rolls = RollMany(Default, B("Turkoman"));
            double good = rolls.Where(p => p.Quality == HorseQuality.Good).Average(p => p.NaturalStats.Speed);
            double bad = rolls.Where(p => p.Quality == HorseQuality.Bad).Average(p => p.NaturalStats.Speed);
            Assert.True(good > 0 && bad < 0 && good > bad);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void UnbreakableStaysUnderTheSetting(string breed)
        {
            double share = RollMany(Default, B(breed)).Count(p => p.Breaking == BreakingDifficulty.Unbreakable) * 100.0 / Rolls;
            Assert.True(share < ModSettings.Defaults().UnbreakableChance, $"{breed}: {share}% unbreakable");
        }

        [Fact]
        public void UnbreakableChanceZeroMeansNeverUnbreakable()
        {
            var roller = new PersonalityRoller(Catalog, With("UnbreakableChance = 0"));
            Assert.DoesNotContain(RollMany(roller, B("Mustang")), p => p.Breaking == BreakingDifficulty.Unbreakable);
        }

        [Fact]
        public void UnbreakableHappensForDifficultBreeds()
        {
            var roller = new PersonalityRoller(Catalog, With("UnbreakableChance = 100"));
            Assert.Contains(RollMany(roller, B("Mustang")), p => p.Breaking == BreakingDifficulty.Unbreakable);
        }

        [Fact]
        public void HardBreakingFollowsTemperMinusBond()
        {
            double mustang = RollMany(Default, B("Mustang")).Count(p => p.Breaking == BreakingDifficulty.Hard) / (double)Rolls;
            double walker = RollMany(Default, B("Tennessee Walker")).Count(p => p.Breaking == BreakingDifficulty.Hard) / (double)Rolls;
            Assert.True(mustang > 0.2, $"Mustang Hard share {mustang}");
            Assert.True(walker < 0.01, $"Tennessee Walker Hard share {walker}");
            foreach (var p in RollMany(Default, B("Mustang")))
            {
                double score = p.Traits.Temper - p.Traits.BondAffinity;
                if (p.Breaking != BreakingDifficulty.Normal) Assert.True(score >= Catalog.Roller.HardBreakScore);
                else Assert.True(score < Catalog.Roller.HardBreakScore);
            }
        }

        [Theory]
        [InlineData(HorseOrigin.Stable)]
        [InlineData(HorseOrigin.Unknown)]
        public void OnlyWildHorsesRollABreakingDifficulty(HorseOrigin origin)
        {
            var roller = new PersonalityRoller(Catalog, With("UnbreakableChance = 100"));
            Assert.All(RollMany(roller, B("Mustang"), origin), p => Assert.Equal(BreakingDifficulty.Normal, p.Breaking));
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void OneOrTwoDifferentQuirks(string breed)
        {
            var rolls = RollMany(Default, B(breed));
            Assert.All(rolls, p =>
            {
                Assert.InRange(p.Quirks.Count, 1, 2);
                Assert.Equal(p.Quirks.Count, p.Quirks.Distinct().Count());
            });
            double two = rolls.Count(p => p.Quirks.Count == 2) / (double)Rolls;
            Assert.InRange(two, Catalog.Roller.SecondQuirkChance - 0.02, Catalog.Roller.SecondQuirkChance + 0.02);
        }

        [Theory]
        [MemberData(nameof(AllBreeds))]
        public void LikelyQuirksComeUpMoreOftenThanOthers(string breed)
        {
            Breed b = B(breed);
            var counts = Enum.GetValues(typeof(Quirk)).Cast<Quirk>().ToDictionary(q => q, q => 0);
            foreach (var p in RollMany(Default, b))
                foreach (Quirk q in p.Quirks) counts[q]++;
            int leastLikely = b.LikelyQuirks.Min(q => counts[q]);
            int mostOther = counts.Where(kv => !b.LikelyQuirks.Contains(kv.Key)).Max(kv => kv.Value);
            Assert.True(leastLikely > mostOther, $"{breed}: a likely quirk came up {leastLikely} times, another quirk {mostOther}");
            Assert.All(counts, kv => Assert.True(kv.Value > 0, $"{breed}: {kv.Key} never came up")); // odds raised, not locked
        }

        [Fact]
        public void WrongSideLimitKeepsTheMostExtremeFlaw()
        {
            var d = new[] { -0.9, -0.3, 0.4, -1.4 };
            PersonalityRoller.LimitWrongSide(d, q: 1, margin: 0.25);
            Assert.Equal(new[] { 0.9, 0.3, 0.4, -1.4 }, d);
            var bad = new[] { 0.9, 0.1, 1.2, -0.5 };
            PersonalityRoller.LimitWrongSide(bad, q: -1, margin: 0.25);
            Assert.Equal(new[] { -0.9, 0.1, 1.2, -0.5 }, bad);
        }

        [Fact]
        public void SeededRandomIsStable()
        {
            // SplitMix64 reference outputs for seed 0.
            var r = new SeededRandom(0);
            Assert.Equal(0xE220A8397B1DCDAFUL, r.NextULong());
            Assert.Equal(0x6E789E6AA1B965F4UL, r.NextULong());
            for (int i = 0; i < 1000; i++) Assert.InRange(new SeededRandom((ulong)i).NextDouble(), 0.0, 0.9999999999999999);
        }
    }

    internal static class Golden
    {
        public const string Turkoman42 = "Turkoman - good horse - seed 000000000000002A | Courage +1.44  Nerves -1.04  Temper +0.26  Bond -0.08 | Quirks: HeadTosser | Natural stats: speed -3.5%, accel +2.8%, health +0.1%, stamina +1.0% | Breaking: Normal";
        public const string ShireDeadBeef = "Shire - good horse - seed 00000000DEADBEEF | Courage +2.00  Nerves -2.00  Temper -0.97  Bond +1.40 | Quirks: PickyEater, ShowOff | Natural stats: speed +1.4%, accel +0.6%, health +0.3%, stamina +3.7% | Breaking: Normal (stable horse)";
    }
}
