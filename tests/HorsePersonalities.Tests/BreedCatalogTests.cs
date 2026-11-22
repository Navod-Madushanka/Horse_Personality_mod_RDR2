using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Logging;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class BreedCatalogTests
    {
        private static BreedCatalog Shipped() => BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());

        [Fact]
        public void ShippedFileHasAll19StoryBreeds()
        {
            var c = Shipped();
            Assert.Equal(19, c.Breeds.Count);
            string[] expected =
            {
                "American Paint", "American Standardbred", "Andalusian", "Appaloosa", "Arabian", "Ardennes", "Belgian Draft",
                "Dutch Warmblood", "Hungarian Halfbred", "Kentucky Saddler", "Missouri Fox Trotter", "Morgan", "Mustang",
                "Nokota", "Shire", "Suffolk Punch", "Tennessee Walker", "Thoroughbred", "Turkoman",
            };
            Assert.Equal(expected, c.Breeds.Select(b => b.Name).OrderBy(n => n));
        }

        [Fact]
        public void EveryShippedBreedHasModelsAndKnownQuirks()
        {
            foreach (var b in Shipped().Breeds)
            {
                Assert.NotEmpty(b.Models);
                Assert.NotEmpty(b.LikelyQuirks);
                Assert.All(b.Models, m => Assert.StartsWith("a_c_horse_", m));
            }
        }

        [Fact]
        public void ShippedFileLoadsWithoutWarnings()
        {
            var log = new MemoryLog();
            BreedCatalog.Load(TestData.BreedsJson, log);
            Assert.DoesNotContain(log.Entries, e => e.Level >= LogLevel.Warn);
        }

        [Theory]
        [InlineData("a_c_horse_turkoman_gold", "Turkoman")]
        [InlineData("a_c_horse_arabian_white", "Arabian")]
        [InlineData("a_c_horse_belgian_blondchestnut", "Belgian Draft")]
        [InlineData("a_c_horse_kentuckysaddle_black", "Kentucky Saddler")]
        [InlineData("a_c_horse_missourifoxtrotter_amberchampagne", "Missouri Fox Trotter")]
        public void FindsBreedByGameModelHash(string model, string breed)
        {
            var c = Shipped();
            Assert.True(c.TryGetBreedByModel(Joaat.Hash(model), out var b));
            Assert.Equal(breed, b.Name);
            Assert.Equal(model, c.DescribeModel(Joaat.Hash(model)));
        }

        [Fact]
        public void OtherModelsAreKnownButHaveNoBreed()
        {
            var c = Shipped();
            uint mule = Joaat.Hash("a_c_horsemule_01");
            Assert.False(c.TryGetBreedByModel(mule, out _));
            Assert.True(c.TryGetOtherModel(mule, out var o));
            Assert.Equal("Mule", o.Note);
            Assert.Equal("0x00000001", c.DescribeModel(1));
        }

        [Fact]
        public void LearningRatesMatchTheSheet()
        {
            var c = Shipped();
            void Check(string name, double p, double k)
            {
                Assert.True(c.TryGetBreed(name, out var b));
                Assert.Equal(p, b.Learning.Physical, 4);
                Assert.Equal(k, b.Learning.Combat, 4);
            }
            Check("Turkoman", 1.17, 1.35);
            Check("Mustang", 0.81, 1.35);
            Check("Missouri Fox Trotter", 1.17, 0.99);
            Check("Arabian", 1.5, 0.8);
            Check("Shire", 0.6, 1.2);
        }

        [Theory]
        [InlineData("Arabian", StatTier.Elite)]
        [InlineData("Thoroughbred", StatTier.Race)]
        [InlineData("Turkoman", StatTier.Standard)]
        [InlineData("Shire", StatTier.Heavy)]
        public void HandlingIsTheStatTier(string breed, StatTier tier)
        {
            Assert.True(Shipped().TryGetBreed(breed, out var b));
            Assert.Equal(tier, b.Tier);
        }

        [Fact]
        public void TiersRunFromHeavyToElite() =>
            Assert.True(StatTier.Heavy < StatTier.Standard && StatTier.Standard < StatTier.Race && StatTier.Race < StatTier.Elite);

        [Fact]
        public void BreakDifficultyIsTemperMinusBondAffinity()
        {
            Assert.True(Shipped().TryGetBreed("Mustang", out var m));
            Assert.Equal(2, m.Leans.BreakDifficulty);
            Assert.True(Shipped().TryGetBreed("Tennessee Walker", out var t));
            Assert.Equal(-3, t.Leans.BreakDifficulty);
        }

        [Fact]
        public void QuirkNamesFromTheSheetNormalize()
        {
            var c = BreedCatalog.Load(TestData.MinimalBreeds, new MemoryLog());
            Assert.Equal(new[] { Quirk.HeadTosser, Quirk.PickyEater, Quirk.ShowOff }, c.Breeds[0].LikelyQuirks);
        }

        [Theory]
        [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 2", "schemaVersion")]
        [InlineData("[\"Race\", \"War\"]", "[\"Race\", \"Pony\"]", "unknown class \"Pony\"")]
        [InlineData("\"courage\": 1", "\"courage\": 3", "outside -2..+2")]
        [InlineData("\"multiClassPenalty\": 0.10", "\"multiClassPenalty\": 1.5", "multiClassPenalty")]
        [InlineData("\"handling\": \"Standard\"", "\"handling\": \"Fast\"", "handling \"Fast\"")]
        [InlineData("\"handling\": \"Standard\", ", "", "handling \"\"")]
        [InlineData("\"a_c_horsemule_01\"", "\"a_c_horse_turkoman_gold\"", "both Turkoman and otherModels")]
        public void RejectsBadData(string find, string replace, string expected)
        {
            string json = TestData.MinimalBreeds.Replace(find, replace);
            var ex = Assert.Throws<BreedDataException>(() => BreedCatalog.Load(json, new MemoryLog()));
            Assert.Contains(ex.Problems, p => p.Contains(expected));
        }

        [Fact]
        public void BrokenJsonIsABreedDataError()
        {
            var ex = Assert.Throws<BreedDataException>(() => BreedCatalog.Load("{ nope", new MemoryLog()));
            Assert.Contains("Not valid JSON", ex.Message);
        }

        [Fact]
        public void UnknownQuirkIsSkippedWithAWarning()
        {
            var log = new MemoryLog();
            var c = BreedCatalog.Load(TestData.MinimalBreeds.Replace("\"Show-off\"", "\"Juggler\""), log);
            Assert.Equal(2, c.Breeds[0].LikelyQuirks.Count);
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains("Juggler"));
        }

        [Fact]
        public void LearningOverrideIsKept()
        {
            string json = TestData.MinimalBreeds.Replace("\"demeanor\"", "\"learning\": { \"physical\": 2.0, \"combat\": 1.35 }, \"demeanor\"");
            var b = BreedCatalog.Load(json, new MemoryLog()).Breeds[0];
            Assert.True(b.LearningOverridden);
            Assert.Equal(2.0, b.Learning.Physical);
        }
    }
}
