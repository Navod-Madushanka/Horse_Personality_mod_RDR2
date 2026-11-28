using System.Linq;
using System;
using HorsePersonalities.Tools.SheetToJson;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class SheetToJsonTests
    {
        private static string Export(string file) => TestData.Read("data", "sheet-export", file);

        /// <summary>Converts the committed CSVs, with any of them swapped for a test's version.</summary>
        private static HorsePersonalities.Core.Json.JsonValue Convert(string breeds = null, string classes = null, string models = null, string roller = null, string tiers = null, string prized = null,
            string behavior = null, string predators = null, string demeanor = null, string coats = null, string treats = null) =>
            SheetConverter.Convert(breeds ?? Export("Breeds.csv"), classes ?? Export("Classes.csv"), models ?? TestData.Read("data", "horse-models.csv"),
                roller ?? Export("Roller.csv"), tiers ?? Export("Tiers.csv"), prized ?? Export("Prized.csv"),
                behavior ?? Export("Behavior.csv"), predators ?? TestData.Read("data", "predator-models.csv"), demeanor ?? Export("Demeanor.csv"),
                coats ?? Export("Coats.csv"), treats ?? TestData.Read("data", "treats.csv"));

        [Fact]
        public void CommittedBreedsJsonMatchesTheSheetExport()
        {
            // If this fails, someone edited the CSVs without regenerating (or edited breeds.json by hand).
            // Fix: dotnet run --project tools/sheet-to-json
            var json = Convert();
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
            var json = Convert(breeds: breeds);
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
                Convert(breeds: breeds));
            Assert.Contains("class \"Pony\"", ex.Message);
            Assert.Contains("Juggler", ex.Message);
        }

        [Fact]
        public void UnknownHandlingIsAnError()
        {
            string breeds = TestData.Read("data", "sheet-export", "Breeds.csv").Replace("\"Nosy, Chatterbox, Head-tosser\",Elite,", "\"Nosy, Chatterbox, Head-tosser\",Superfast,");
            var ex = Assert.Throws<ConversionException>(() =>
                Convert(breeds: breeds));
            Assert.Contains("Handling \"Superfast\"", ex.Message);
        }

        [Fact]
        public void RollerAndTierNumbersComeFromTheirTabs()
        {
            var json = Convert();
            Assert.Equal(0.35, json["roller"]["secondQuirkChance"].AsNumber());
            Assert.Equal(0.6, json["roller"]["traitSpread"].AsNumber());
            Assert.Equal(1, json["tiers"]["Standard"]["statUpRanks"].AsInt());
            Assert.Equal(2, json["tiers"]["Standard"]["statDownRanks"].AsInt());
            Assert.Equal(0, json["tiers"]["Elite"]["statDownRanks"].AsInt());
        }

        [Theory]
        [InlineData("Trait spread,0.6,", "Trait wobble,0.6,", "unknown setting \"Trait wobble\"")]
        [InlineData("Stat spread,0.6,", "Stat spread,lots,", "Stat spread \"lots\"")]
        [InlineData("Hard break score,1.5,", "", "no \"Hard break score\" row")]
        public void BadRollerTabIsAnError(string find, string replace, string expected)
        {
            string roller = Export("Roller.csv");
            if (replace.Length == 0) roller = string.Join("\n", roller.Split('\n').Where(l => !l.StartsWith(find)));
            else roller = roller.Replace(find, replace);
            var ex = Assert.Throws<ConversionException>(() => Convert(roller: roller));
            Assert.Contains(expected, ex.Message);
        }

        [Theory]
        [InlineData("Heavy,1,2,", "Lumbering,1,2,", "unknown tier \"Lumbering\"")]
        [InlineData("Elite,1,0,", "Elite,1,,", "Elite needs both caps")]
        [InlineData("Race,1,1,", "Race,1.5,1,", "whole ranks")]
        [InlineData("Race,1,1,", "Race,12,1,", "whole ranks")]
        public void BadTiersTabIsAnError(string find, string replace, string expected)
        {
            string tiers = Export("Tiers.csv").Replace(find, replace);
            Exception ex = Record.Exception(() => HorsePersonalities.Core.Breeds.BreedCatalog.Load(Convert(tiers: tiers).ToJson(), new HorsePersonalities.Core.Logging.MemoryLog()));
            Assert.NotNull(ex);
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void PrizedTabBecomesPrizedModels()
        {
            var p = Convert()["prizedModels"];
            Assert.Equal(14, p.Count);
            Assert.Contains(p.Items, m => m["model"].AsString() == "a_c_horse_arabian_redchestnut_pc" && m["why"].AsString() == "Special");
        }

        [Theory]
        [InlineData("a_c_horse_arabian_redchestnut_pc,Arabian,Special", "a_c_horse_gang_dutch,Arabian,Special", "stay vanilla anyway")]
        [InlineData("a_c_horse_arabian_redchestnut_pc,Arabian,Special", "a_c_horse_arabian_redchestnut_pc,Arabian,Legendary", "Special, Rare or Community favorite")]
        [InlineData("a_c_horse_morgan_liverchestnut_pc,Morgan,Special", "a_c_horse_appaloosa_fewspotted_pc,Appaloosa,Rare", "listed twice")]
        public void BadPrizedTabIsAnError(string find, string replace, string expected)
        {
            var ex = Assert.Throws<ConversionException>(() => Convert(prized: Export("Prized.csv").Replace(find, replace)));
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void CommunityFavoriteIsReadFromTheSheetsWording()
        {
            // The shipped Prized tab writes "Community favorite" (the Gold Turkoman and the Silver Dapple Pinto).
            Assert.Contains("Community favorite", Export("Prized.csv"));
            Assert.Contains(Convert()["prizedModels"].Items, m => m["model"].AsString() == "a_c_horse_turkoman_gold" && m["why"].AsString() == "CommunityFavorite");
        }

        [Fact]
        public void BehaviorTabAndPredatorsAreConverted()
        {
            var json = Convert();
            Assert.Equal(0.01, json["behavior"]["spookBaseRate"].AsNumber());
            Assert.Equal(20, json["behavior"]["threatQuietSeconds"].AsInt());
            Assert.Contains(json["predatorModels"].Items, m => m.AsString() == "a_c_wolf_01");
        }

        [Theory]
        [InlineData("Spook base rate,0.01,", "Spook rate,0.01,", "unknown setting \"Spook rate\"")]
        [InlineData("Behavior gap,20,", "", "no \"Behavior gap\" row")]
        public void BadBehaviorTabIsAnError(string find, string replace, string expected)
        {
            string tab = Export("Behavior.csv");
            tab = replace.Length == 0 ? string.Join("\n", tab.Split('\n').Where(l => !l.StartsWith(find))) : tab.Replace(find, replace);
            var ex = Assert.Throws<ConversionException>(() => Convert(behavior: tab));
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void PredatorListMustBeAnimals()
        {
            var ex = Assert.Throws<ConversionException>(() => Convert(predators: TestData.Read("data", "predator-models.csv") + "a_c_horse_arabian_white,horse?\nmp_male,person\n"));
            Assert.Contains("mp_male is not an animal model", ex.Message);
        }

        [Fact]
        public void DemeanorTabBecomesEachBreedsDemeanorTraits()
        {
            var turkoman = Convert()["breeds"].Items.Single(b => b["name"].AsString() == "Turkoman");
            Assert.Equal(2, turkoman["demeanorTraits"]["impatient"].AsInt());
            Assert.Equal(2, turkoman["demeanorTraits"]["fussy"].AsInt());
        }

        [Fact]
        public void EveryBreedNeedsADemeanorRow()
        {
            string tab = string.Join("\n", Export("Demeanor.csv").Split('\n').Where(l => !l.StartsWith("Morgan,")));
            Assert.Contains("Morgan", Assert.Throws<ConversionException>(() => Convert(demeanor: tab)).Message);
            Assert.Contains("from 0 to 2", Assert.Throws<ConversionException>(() => Convert(demeanor: Export("Demeanor.csv").Replace("Turkoman,2,", "Turkoman,5,"))).Message);
        }

        [Fact]
        public void ShowOffIsGoneFromEveryBreed()
        {
            foreach (var b in Convert()["breeds"].Items)
                Assert.DoesNotContain(b["likelyQuirks"].Items, q => q.AsString() == "ShowOff");
        }

        [Fact]
        public void ModelForAMissingBreedIsAnError()
        {
            string models = TestData.Read("data", "horse-models.csv") + "a_c_horse_fake_01,Pegasus,,\n";
            var ex = Assert.Throws<ConversionException>(() =>
                Convert(models: models));
            Assert.Contains("Pegasus", ex.Message);
        }
    }
}
