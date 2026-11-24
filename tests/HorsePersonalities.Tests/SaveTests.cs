using System;
using System.IO;
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
    /// <summary>Build plan, Phase 4: round trip, broken file, missing file, backup, reset.</summary>
    public sealed class SaveTests : IDisposable
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly DateTime Now = new DateTime(2026, 11, 24, 21, 30, 5);
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "hp-save-" + Guid.NewGuid().ToString("N"));
        private readonly MemoryLog _log = new MemoryLog();

        public SaveTests() => Directory.CreateDirectory(_dir);
        public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

        private HorseSaveStore Store() => new HorseSaveStore(_dir, _log);
        private string P(string f) => Path.Combine(_dir, f);

        private static HorseRegistry RegistryWith(params (string Model, string Breed)[] horses)
        {
            ulong seed = 0xABCDEF0123456789UL;
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => seed++, () => Now);
            int handle = 1;
            foreach (var (model, breed) in horses)
            {
                Catalog.TryGetBreed(breed, out Breed b);
                reg.OnMount(handle, new HorseFingerprint(model, handle % 2 == 0), b, HorseOrigin.Wild, false);
                reg.OnSaddle(handle, new HorseFingerprint(model, handle % 2 == 0), b);
                handle++;
            }
            return reg;
        }

        private static readonly (string, string)[] FiveHorses =
        {
            ("a_c_horse_turkoman_gold", "Turkoman"), ("a_c_horse_arabian_white", "Arabian"), ("a_c_horse_shire_ravenblack", "Shire"),
            ("a_c_horse_mustang_wildbay", "Mustang"), ("a_c_horse_morgan_bay", "Morgan"),
        };

        [Fact]
        public void RoundTripKeepsEveryValue()
        {
            var reg = RegistryWith(FiveHorses);
            string text = HorseSaveFormat.Write(reg.Permanent);
            var back = HorseSaveFormat.Read(text, _log);
            Assert.Equal(5, back.Count);
            foreach (var original in reg.Permanent)
            {
                var r = back.Single(x => x.Id.Equals(original.Id));
                Assert.Equal(original.Personality.ToString(), r.Personality.ToString());
                Assert.Equal(original.Personality.NaturalStats.Acceleration, r.Personality.NaturalStats.Acceleration); // exact, not rounded
                Assert.Equal(original.Personality.Seed, r.Personality.Seed);
                Assert.Equal(original.Personality.Origin, r.Personality.Origin);
                Assert.Equal(original.Personality.Quirks, r.Personality.Quirks);
                Assert.Equal(original.Personality.Traits.Temper, r.Personality.Traits.Temper);
                Assert.Equal(original.FirstSeen, r.FirstSeen);
            }
            Assert.Equal(text, HorseSaveFormat.Write(back)); // writing what was read changes nothing
        }

        [Fact]
        public void WritesThePlansFormat()
        {
            var json = HorsePersonalities.Core.Json.JsonValue.Parse(HorseSaveFormat.Write(RegistryWith(("a_c_horse_turkoman_gold", "Turkoman")).Permanent));
            Assert.Equal(1, json["schemaVersion"].AsInt());
            var h = json["horses"]["a_c_horse_turkoman_gold|female|default"];
            Assert.Equal("Turkoman", h["breed"].AsString());
            Assert.Equal("Wild", h["origin"].AsString());
            Assert.Equal("Permanent", h["status"].AsString());
            foreach (string k in new[] { "courage", "nerves", "temper", "bondAffinity" }) Assert.Equal(JsonKindNumber, h["traits"][k].Kind);
            Assert.Equal("ABCDEF0123456789", h["seed"].AsString());
            Assert.Equal("2026-11-24", h["firstSeen"].AsString());
        }

        private const HorsePersonalities.Core.Json.JsonKind JsonKindNumber = HorsePersonalities.Core.Json.JsonKind.Number;

        [Fact]
        public void TemporaryPersonalitiesAreNeverWritten()
        {
            ulong seed = 1;
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => seed++, () => Now);
            Catalog.TryGetBreed("Turkoman", out Breed t);
            reg.OnMount(1, new HorseFingerprint("a_c_horse_turkoman_gold", true), t, HorseOrigin.Unknown, false);
            Assert.DoesNotContain("turkoman", HorseSaveFormat.Write(reg.Permanent));
        }

        [Fact]
        public void FiveHorsesSurviveThreeSessions()
        {
            var first = RegistryWith(FiveHorses);
            Assert.True(Store().Save(first.Permanent));
            string expected = string.Join("\n", first.Permanent.OrderBy(r => r.Id.Key).Select(r => r.Personality.ToString()));
            for (int session = 0; session < 3; session++)
            {
                var loaded = Store().Load(false, Now);
                Assert.Equal(SaveSource.Main, loaded.Source);
                var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog());
                reg.Load(loaded.Records);
                Assert.Equal(expected, string.Join("\n", reg.Permanent.OrderBy(r => r.Id.Key).Select(r => r.Personality.ToString())));
                Assert.True(Store().Save(reg.Permanent));
            }
        }

        [Fact]
        public void EachWriteKeepsThePreviousFileAsTheBackup()
        {
            Store().Save(RegistryWith(FiveHorses.Take(1).ToArray()).Permanent);
            Assert.False(File.Exists(P("horses.backup.json")));
            Store().Save(RegistryWith(FiveHorses).Permanent);
            Assert.Single(HorseSaveFormat.Read(File.ReadAllText(P("horses.backup.json")), _log));
            Assert.Equal(5, HorseSaveFormat.Read(File.ReadAllText(P("horses.json")), _log).Count);
            Assert.False(File.Exists(P("horses.json.tmp")));
        }

        [Fact]
        public void MissingFileStartsEmpty()
        {
            var r = Store().Load(false, Now);
            Assert.Equal(SaveSource.Empty, r.Source);
            Assert.Empty(r.Records);
            Assert.DoesNotContain(_log.Entries, e => e.Level == LogLevel.Error);
        }

        [Theory]
        [InlineData("{ this is not json")]
        [InlineData("")]
        [InlineData("{\"schemaVersion\": 99, \"horses\": {}}")]
        public void BrokenFileIsKeptAsideAndTheBackupLoads(string broken)
        {
            Store().Save(RegistryWith(FiveHorses.Take(2).ToArray()).Permanent);
            Store().Save(RegistryWith(FiveHorses).Permanent); // backup now holds the 2-horse save
            File.WriteAllText(P("horses.json"), broken);

            var r = Store().Load(false, Now);
            Assert.Equal(SaveSource.Backup, r.Source);
            Assert.Equal(2, r.Records.Count);
            Assert.Equal(broken, File.ReadAllText(P("horses.broken-20261124-213005.json"))); // nothing deleted
            Assert.Contains(_log.Entries, e => e.Level == LogLevel.Error);
        }

        [Fact]
        public void DeletedMainFileFallsBackToTheBackup()
        {
            Store().Save(RegistryWith(FiveHorses.Take(2).ToArray()).Permanent);
            Store().Save(RegistryWith(FiveHorses).Permanent);
            File.Delete(P("horses.json"));
            var r = Store().Load(false, Now);
            Assert.Equal(SaveSource.Backup, r.Source);
            Assert.Equal(2, r.Records.Count);
        }

        [Fact]
        public void BrokenFileAndBrokenBackupStartEmptyWithoutCrashing()
        {
            File.WriteAllText(P("horses.json"), "garbage");
            File.WriteAllText(P("horses.backup.json"), "more garbage");
            var r = Store().Load(false, Now);
            Assert.Equal(SaveSource.Empty, r.Source);
            Assert.True(File.Exists(P("horses.broken-20261124-213005.json")));
        }

        [Fact]
        public void OneBadRecordIsSkippedAndTheRestLoad()
        {
            string text = HorseSaveFormat.Write(RegistryWith(FiveHorses).Permanent)
                .Replace("\"breed\": \"Shire\"", "\"breed\": \"\"");
            var records = HorseSaveFormat.Read(text, _log);
            Assert.Equal(4, records.Count);
            Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains("shire"));
        }

        [Theory]
        [InlineData("\"quality\": \"Good\"", "\"quality\": \"Great\"")]
        [InlineData("\"courage\": ", "\"courage\": 9, \"x\": ")]
        public void BadValuesInARecordSkipThatRecord(string find, string replace)
        {
            string text = HorseSaveFormat.Write(RegistryWith(FiveHorses).Permanent);
            int at = text.IndexOf(find, StringComparison.Ordinal);
            Assume(at >= 0);
            text = text.Substring(0, at) + replace + text.Substring(at + find.Length);
            Assert.Equal(4, HorseSaveFormat.Read(text, _log).Count);
        }

        private static void Assume(bool condition) { if (!condition) throw new Xunit.Sdk.XunitException("Test setup: text not found"); }

        [Fact]
        public void ResetMovesTheSaveAsideAndStartsEmpty()
        {
            Store().Save(RegistryWith(FiveHorses).Permanent);
            var r = Store().Load(reset: true, Now);
            Assert.Equal(SaveSource.Reset, r.Source);
            Assert.Empty(r.Records);
            Assert.False(File.Exists(P("horses.json")));
            Assert.Equal(5, HorseSaveFormat.Read(File.ReadAllText(P("horses.reset-20261124-213005.json")), _log).Count);
        }

        [Fact]
        public void ResetSettingTurnsItselfOff()
        {
            string ini = ModSettings.DefaultIniText().Replace("ResetAllHorses = false", "ResetAllHorses = true ; new playthrough");
            Assert.True(ModSettings.Load(ini, new MemoryLog()).ResetAllHorses);
            string after = IniEdit.SetValue(ini, "ResetAllHorses", "false");
            Assert.False(ModSettings.Load(after, new MemoryLog()).ResetAllHorses);
            Assert.Contains("ResetAllHorses = false ; new playthrough", after);
            Assert.Equal(ini.Split('\n').Length, after.Split('\n').Length);
            Assert.Contains("; Wipes saved personalities on next load", after); // comments kept
        }

        [Fact]
        public void IniEditLeavesOtherKeysAndCommentedLinesAlone()
        {
            const string ini = "; ResetAllHorses = true\r\n[HorsePersonalities]\r\nGoodHorseChance = 70\r\nresetallhorses=true\r\n";
            string after = IniEdit.SetValue(ini, "ResetAllHorses", "false");
            Assert.Equal("; ResetAllHorses = true\r\n[HorsePersonalities]\r\nGoodHorseChance = 70\r\nresetallhorses = false\r\n", after);
            Assert.Equal("GoodHorseChance = 70", IniEdit.SetValue("GoodHorseChance = 70", "Missing", "1"));
        }
    }
}
