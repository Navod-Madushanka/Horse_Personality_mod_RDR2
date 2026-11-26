using System;
using System.IO;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Saving;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Testing;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Test mode (decided Nov 26, 2026).</summary>
    public class TestModeTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly TestPresets Shipped = TestPresets.Load(TestData.Read("data", "test-personalities.json"), new MemoryLog());
        private static Breed B(string n) { Catalog.TryGetBreed(n, out Breed b); return b; }

        [Fact]
        public void ShippedPresetsCoverEveryQuirkAndEveryCase()
        {
            var all = Shipped.Presets.Select(p => p.For(B("Morgan"), HorseOrigin.Stable, 1)).ToList();
            foreach (Quirk q in Enum.GetValues(typeof(Quirk)))
                Assert.Contains(all, p => p.Quirks.Contains(q));
            Assert.Contains(all, p => p.Traits.Courage == -2);
            Assert.Contains(all, p => p.Traits.Courage == 2);
            Assert.Contains(all, p => p.Traits.Nerves == 2);
            Assert.Contains(all, p => p.Traits.Temper == 2);
            Assert.Contains(all, p => p.Traits.BondAffinity == -2);
            Assert.Contains(all, p => p.Breaking == BreakingDifficulty.Hard && p.Origin == HorseOrigin.Wild);
            Assert.Contains(all, p => p.Breaking == BreakingDifficulty.Unbreakable && p.Origin == HorseOrigin.Wild);
            Assert.Contains(all, p => p.Prized && p.NaturalStats.Health == 1 && p.NaturalStats.Stamina == -2);
            Assert.Equal("Average", Shipped.Presets[0].Name);
            Assert.All(Shipped.Presets, p => Assert.False(string.IsNullOrWhiteSpace(p.Purpose)));
        }

        [Fact]
        public void NextCyclesThroughEveryPresetAndWraps()
        {
            string name = "Average";
            var seen = new System.Collections.Generic.List<string> { name };
            for (int i = 0; i < Shipped.Presets.Count; i++) seen.Add(name = Shipped.Next(name).Name);
            Assert.Equal("Average", seen.Last());
            Assert.Equal(Shipped.Presets.Count, seen.Distinct().Count());
            Assert.Equal("Average", Shipped.Next(null).Name);
        }

        [Fact]
        public void AssignGivesAModelItsPreset()
        {
            const string json = "{\"presets\":[{\"name\":\"Average\",\"traits\":{}},{\"name\":\"Coward\",\"traits\":{\"courage\":-2}}],\"assign\":{\"A_C_Horse_Morgan_Bay\":\"Coward\"}}";
            var presets = TestPresets.Load(json, new MemoryLog());
            Assert.Equal("Coward", presets.ForModel("a_c_horse_morgan_bay").Name);
            Assert.Equal("Average", presets.ForModel("a_c_horse_shire_ravenblack").Name);
        }

        [Theory]
        [InlineData("{\"presets\":[]}", "no presets")]
        [InlineData("{\"presets\":[{\"name\":\"X\",\"traits\":{\"courage\":5}}]}", "outside -2..+2")]
        [InlineData("{\"presets\":[{\"name\":\"X\",\"traits\":{},\"quirks\":[\"ShowOff\"]}]}", "unknown quirk")]
        [InlineData("{\"presets\":[{\"name\":\"X\",\"traits\":{}}],\"assign\":{\"a_c_horse_morgan_bay\":\"Y\"}}", "names no preset")]
        [InlineData("{ nope", "not valid JSON")]
        public void BadPresetFilesAreRefusedClearly(string json, string expected)
        {
            var ex = Assert.Throws<BreedDataException>(() => TestPresets.Load(json, new MemoryLog()));
            Assert.Contains(expected, ex.Message);
        }

        private static HorseRegistry TestRegistry()
        {
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => 5, () => new DateTime(2026, 11, 26));
            reg.UseInsteadOfRoller((id, breed, origin, seed) => { var p = Shipped.ForModel(id.Model); return (p.For(breed, origin, seed), p.Name); });
            return reg;
        }

        [Fact]
        public void TestModeGivesEveryNewHorseItsPresetInsteadOfARoll()
        {
            var reg = TestRegistry();
            var temp = reg.OnMount(1, new HorseFingerprint("a_c_horse_morgan_bay", true), B("Morgan"), HorseOrigin.Unknown, false);
            var kept = reg.OnSaddle(2, new HorseFingerprint("a_c_horse_shire_ravenblack", true), B("Shire"));
            Assert.Equal("Average", temp.Preset);
            Assert.Equal("Average", kept.Preset);
            Assert.All(new[] { temp, kept }, r => Assert.Equal(0, r.Personality.Traits.Courage));
        }

        [Fact]
        public void ReplaceSwapsThePersonalityAndKeepsTheRest()
        {
            var reg = TestRegistry();
            var id = new HorseFingerprint("a_c_horse_shire_ravenblack", true);
            var kept = reg.OnSaddle(2, id, B("Shire"));
            reg.RememberVanillaBase(kept, "health", 4);
            reg.MarkSaved();
            TestPreset coward = Shipped.Find("Coward");
            var replaced = reg.Replace(2, id, coward.For(B("Shire"), kept.Personality.Origin, kept.Personality.Seed), coward.Name);
            Assert.Equal(-2, replaced.Personality.Traits.Courage);
            Assert.Equal("Coward", replaced.Preset);
            Assert.Equal(4, replaced.VanillaBase["health"]);
            Assert.Equal(PersonalityStatus.Permanent, replaced.Status);
            Assert.True(reg.Dirty);
            Assert.Same(replaced, reg.Find(2, id));
            Assert.Null(reg.Replace(9, new HorseFingerprint("a_c_horse_morgan_bay", true), coward.For(B("Morgan"), HorseOrigin.Stable, 1), "Coward"));
        }

        [Fact]
        public void APresetsWildOriginMakesBreakingTestableOnAnyHorse()
        {
            var p = Shipped.Find("Wild Unbreakable").For(B("Morgan"), HorseOrigin.Stable, 1);
            Assert.Equal(HorseOrigin.Wild, p.Origin);
            Assert.Equal(BreakingDifficulty.Unbreakable, p.Breaking);
        }

        [Fact]
        public void PresetNameIsSavedAndTheTestSaveIsItsOwnFile()
        {
            string dir = Path.Combine(Path.GetTempPath(), "hp-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var reg = TestRegistry();
                reg.OnSaddle(2, new HorseFingerprint("a_c_horse_shire_ravenblack", true), B("Shire"));
                var store = new HorseSaveStore(dir, new MemoryLog(), name: "horses.test");
                Assert.True(store.Save(reg.Permanent));
                Assert.True(File.Exists(Path.Combine(dir, "horses.test.json")));
                Assert.False(File.Exists(Path.Combine(dir, "horses.json")));
                Assert.Equal("Average", store.Load(false, DateTime.Now).Records.Single().Preset);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Fact]
        public void TestModeSettingsDefaultOff()
        {
            var d = ModSettings.Defaults();
            Assert.False(d.TestPersonalities);
            Assert.Equal("F11", d.TestPresetKey);
            var on = ModSettings.Load("TestPersonalities = true\nTestPresetKey = F7\n", new MemoryLog());
            Assert.True(on.TestPersonalities);
            Assert.Equal("F7", on.TestPresetKey);
        }
    }
}
