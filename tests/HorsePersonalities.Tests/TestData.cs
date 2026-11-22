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
  ""schemaVersion"": 1,
  ""multiClassPenalty"": 0.10,
  ""classes"": { ""Race"": { ""physical"": 1.3, ""combat"": 0.7 }, ""War"": { ""physical"": 0.8, ""combat"": 1.5 } },
  ""breeds"": [
    { ""name"": ""Turkoman"", ""models"": [""a_c_horse_turkoman_gold"", ""a_c_horse_turkoman_silver""], ""classes"": [""Race"", ""War""],
      ""handling"": ""Standard"", ""leans"": { ""courage"": 1, ""nerves"": 0, ""temper"": 1, ""bondAffinity"": 0 },
      ""likelyQuirks"": [""HeadTosser"", ""Picky eater"", ""Show-off""], ""demeanor"": ""Proud and impatient."" }
  ],
  ""otherModels"": [ { ""model"": ""a_c_horsemule_01"", ""note"": ""Mule"" } ]
}";
    }
}
