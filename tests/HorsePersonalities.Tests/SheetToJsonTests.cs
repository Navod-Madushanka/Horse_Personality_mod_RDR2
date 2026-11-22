using HorsePersonalities.Tools.SheetToJson;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class SheetToJsonTests
    {
        [Fact]
        public void CommittedBreedsJsonMatchesTheSheetExport()
        {
            // If this fails, someone edited the CSVs without regenerating (or edited breeds.json by hand).
            // Fix: dotnet run --project tools/sheet-to-json
            var json = SheetConverter.Convert(
                TestData.Read("data", "sheet-export", "Breeds.csv"),
                TestData.Read("data", "sheet-export", "Classes.csv"),
                TestData.Read("data", "horse-models.csv"));
            Assert.Equal(TestData.BreedsJson.Replace("\r\n", "\n").Trim(), json.ToJson().Trim());
        }

        [Theory]
        [InlineData("+1", 1)]
        [InlineData("-2", -2)]
        [InlineData("0.90", 0.9)]
        [InlineData("1,5", 1.5)]
        [InlineData("1.1700000000000002", 1.1700000000000002)]
        [InlineData("−1", -1)] // the sheet can export a real minus sign
        public void ReadsSheetNumbers(string raw, double expected)
        {
            Assert.True(SheetConverter.TryNumber(raw, out double v));
            Assert.Equal(expected, v, 10);
        }

        [Theory]
        [InlineData("10%", 0.10)]
        [InlineData("0.1", 0.1)]
        [InlineData("15", 0.15)]
        public void ReadsThePenalty(string raw, double expected) => Assert.Equal(expected, SheetConverter.ParseFraction(raw), 10);

        [Fact]
        public void ParsesQuotedCsv()
        {
            var rows = Csv.Parse("a,\"b, c\",\"say \"\"hi\"\"\"\r\n\"multi\nline\",x\n");
            Assert.Equal(new[] { "a", "b, c", "say \"hi\"" }, rows[0]);
            Assert.Equal(new[] { "multi\nline", "x" }, rows[1]);
        }

        [Fact]
        public void TypedOverLearningRateBecomesAnOverride()
        {
            string breeds = TestData.Read("data", "sheet-export", "Breeds.csv").Replace("Turkoman,Race,War,1.17,", "Turkoman,Race,War,1.40,");
            var json = SheetConverter.Convert(breeds, TestData.Read("data", "sheet-export", "Classes.csv"), TestData.Read("data", "horse-models.csv"));
            foreach (var b in json["breeds"].Items)
                if (b["name"].AsString() == "Turkoman")
                {
                    Assert.Equal(1.4, b["learning"]["physical"].AsNumber());
                    Assert.Equal(1.35, b["learning"]["combat"].AsNumber());
                    return;
                }
            Assert.Fail("Turkoman missing");
        }

        [Fact]
        public void ReportsEveryProblemAtOnce()
        {
            string breeds = TestData.Read("data", "sheet-export", "Breeds.csv")
                .Replace("Arabian,Superior,", "Arabian,Pony,")
                .Replace("\"Shadow, Wanderer\"", "\"Shadow, Juggler\"");
            var ex = Assert.Throws<ConversionException>(() =>
                SheetConverter.Convert(breeds, TestData.Read("data", "sheet-export", "Classes.csv"), TestData.Read("data", "horse-models.csv")));
            Assert.Contains("class \"Pony\"", ex.Message);
            Assert.Contains("Juggler", ex.Message);
        }

        [Fact]
        public void UnknownHandlingIsAnError()
        {
            string breeds = TestData.Read("data", "sheet-export", "Breeds.csv").Replace("\"Nosy, Chatterbox, Head-tosser\",Elite,", "\"Nosy, Chatterbox, Head-tosser\",Superfast,");
            var ex = Assert.Throws<ConversionException>(() =>
                SheetConverter.Convert(breeds, TestData.Read("data", "sheet-export", "Classes.csv"), TestData.Read("data", "horse-models.csv")));
            Assert.Contains("Handling \"Superfast\"", ex.Message);
        }

        [Fact]
        public void ModelForAMissingBreedIsAnError()
        {
            string models = TestData.Read("data", "horse-models.csv") + "a_c_horse_fake_01,Pegasus,,\n";
            var ex = Assert.Throws<ConversionException>(() =>
                SheetConverter.Convert(TestData.Read("data", "sheet-export", "Breeds.csv"), TestData.Read("data", "sheet-export", "Classes.csv"), models));
            Assert.Contains("Pegasus", ex.Message);
        }
    }
}
