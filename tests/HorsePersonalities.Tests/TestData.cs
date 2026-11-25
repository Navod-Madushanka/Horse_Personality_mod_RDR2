using System.IO;

namespace HorsePersonalities.Tests
{
    internal static class TestData
    {
        public static string RepoRoot
        {
            get
            {
                string dir = Directory.GetCurrentDirectory();
                while (dir != null && !File.Exists(Path.Combine(dir, "HorsePersonalities.sln"))) dir = Path.GetDirectoryName(dir);
                return dir ?? throw new DirectoryNotFoundException("Could not find the repo root (HorsePersonalities.sln).");
            }
        }

        public static string Read(params string[] parts) => File.ReadAllText(Path.Combine(RepoRoot, Path.Combine(parts)));

        public static string BreedsJson => Read("data", "breeds.json");

        /// <summary>A tiny valid breeds.json to edit in tests.</summary>
        public const string MinimalBreeds = @"{
  ""schemaVersion"": 3,
  ""multiClassPenalty"": 0.10,
  ""roller"": { ""traitSpread"": 0.6, ""qualityShift"": 0.5, ""flawMargin"": 0.25, ""secondQuirkChance"": 0.35, ""likelyQuirkWeight"": 4,
                ""statSpread"": 0.6, ""statQualityShift"": 0.3, ""hardBreakScore"": 1.5, ""unbreakableBreakScore"": 2.5 },
  ""tiers"": { ""Heavy"": { ""statUpRanks"": 1, ""statDownRanks"": 2 }, ""Standard"": { ""statUpRanks"": 1, ""statDownRanks"": 2 },
               ""Race"": { ""statUpRanks"": 1, ""statDownRanks"": 1 }, ""Elite"": { ""statUpRanks"": 1, ""statDownRanks"": 0 } },
  ""classes"": { ""Race"": { ""physical"": 1.3, ""combat"": 0.7 }, ""War"": { ""physical"": 0.8, ""combat"": 1.5 } },
  ""breeds"": [
    { ""name"": ""Turkoman"", ""models"": [""a_c_horse_turkoman_gold"", ""a_c_horse_turkoman_silver""], ""classes"": [""Race"", ""War""],
      ""handling"": ""Standard"", ""leans"": { ""courage"": 1, ""nerves"": 0, ""temper"": 1, ""bondAffinity"": 0 },
      ""likelyQuirks"": [""Head-tosser"", ""Picky eater"", ""nosy""], ""demeanor"": ""Proud and impatient."" }
  ],
  ""otherModels"": [ { ""model"": ""a_c_horsemule_01"", ""note"": ""Mule"" } ]
}";
    }
}
