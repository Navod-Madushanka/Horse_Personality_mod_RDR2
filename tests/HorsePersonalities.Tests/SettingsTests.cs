using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void MissingFileUsesDefaults()
        {
            var s = ModSettings.Load(null, new MemoryLog());
            Assert.Equal(80, s.GoodHorseChance);
            Assert.Equal(5, s.UnbreakableChance);
            Assert.Equal(2, s.MaxStatRankChange);
            Assert.False(s.RestoreVanillaStats);
            Assert.Equal(1.0, s.SpookFrequency);
            Assert.Equal(120, s.QuirkCooldownSeconds);
            Assert.True(s.DisableDuringMissions);
            Assert.False(s.DebugOverlay);
            Assert.Equal("F9", s.DebugKey);
            Assert.False(s.ResetAllHorses);
        }

        [Fact]
        public void DefaultFileReadsBackAsDefaultsWithNoWarnings()
        {
            var log = new MemoryLog();
            var s = ModSettings.Load(ModSettings.DefaultIniText(), log);
            Assert.Empty(log.Entries);
            Assert.Equal(80, s.GoodHorseChance);
            Assert.Equal(LogLevel.Info, s.LogLevel);
        }

        [Fact]
        public void ReadsValidValues()
        {
            const string ini = "[HorsePersonalities]\nGoodHorseChance = 65 ; fewer good horses\nSpookFrequency=1,5\nDebugOverlay = yes\nDebugKey = F10\nLogLevel = debug\n";
            var log = new MemoryLog();
            var s = ModSettings.Load(ini, log);
            Assert.Empty(log.Entries);
            Assert.Equal(65, s.GoodHorseChance);
            Assert.Equal(1.5, s.SpookFrequency);
            Assert.True(s.DebugOverlay);
            Assert.Equal("F10", s.DebugKey);
            Assert.Equal(LogLevel.Debug, s.LogLevel);
        }

        [Theory]
        [InlineData("GoodHorseChance = lots", "GoodHorseChance")]
        [InlineData("GoodHorseChance = 150", "outside 0-100")]
        [InlineData("SpookFrequency = -1", "SpookFrequency")]
        [InlineData("DebugOverlay = maybe", "not true or false")]
        [InlineData("LogLevel = Loud", "LogLevel")]
        [InlineData("DebugKey = F 9", "not a key name")]
        public void BadValuesFallBackWithALogLine(string line, string expectedInLog)
        {
            var log = new MemoryLog();
            var s = ModSettings.Load("[HorsePersonalities]\n" + line + "\n", log);
            var d = ModSettings.Defaults();
            Assert.Equal(d.GoodHorseChance, s.GoodHorseChance);
            Assert.Equal(d.SpookFrequency, s.SpookFrequency);
            Assert.Equal(d.DebugOverlay, s.DebugOverlay);
            Assert.Equal(d.LogLevel, s.LogLevel);
            Assert.Equal(d.DebugKey, s.DebugKey);
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains(expectedInLog));
        }

        [Fact]
        public void TheOldPercentSettingIsIgnoredWithAWarning()
        {
            var log = new MemoryLog();
            var s = ModSettings.Load("[HorsePersonalities]\nMaxStatVariance = 10\n", log);
            Assert.Equal(2, s.MaxStatRankChange);
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains("MaxStatRankChange"));
        }

        [Fact]
        public void KeyOutsideTheSectionStillCounts()
        {
            var s = ModSettings.Load("GoodHorseChance = 50\n", new MemoryLog());
            Assert.Equal(50, s.GoodHorseChance);
        }

        [Fact]
        public void MalformedLinesAreWarnedNotFatal()
        {
            var log = new MemoryLog();
            var s = ModSettings.Load("[HorsePersonalities\nthis is not a setting\nGoodHorseChance = 70\n", log);
            Assert.Equal(70, s.GoodHorseChance);
            Assert.Equal(2, log.Entries.FindAll(e => e.Level == LogLevel.Warn).Count);
        }
            // ---- Phase 9: settings check ----

        [Fact]
        public void ATypoGetsAWarningWithTheClosestSetting()
        {
            var log = new MemoryLog();
            var s = ModSettings.Load("[HorsePersonalities]\nSpookFrequncy = 3\n", log);
            Assert.Equal(1.0, s.SpookFrequency); // the typo didn't set anything
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains("unknown setting \"SpookFrequncy\"") && e.Message.Contains("Did you mean SpookFrequency?"));
        }

        [Fact]
        public void TheDefaultFileHasNoUnknownSettingsOrReminders()
        {
            var log = new MemoryLog();
            var s = ModSettings.Load(ModSettings.DefaultIniText(), log);
            Assert.DoesNotContain(log.Entries, e => e.Message.Contains("unknown setting"));
            Assert.Empty(s.TestingReminders());
        }

        [Fact]
        public void TestingSettingsLeftOnAreRemindedOf()
        {
            var s = ModSettings.Load("[HorsePersonalities]\nTestPersonalities = true\nExperienceSpeed = 20\nRestoreVanillaStats = true\n", new MemoryLog());
            var r = s.TestingReminders();
            Assert.Contains(r, x => x.StartsWith("TestPersonalities is on"));
            Assert.Contains(r, x => x.StartsWith("ExperienceSpeed is 20"));
            Assert.Contains(r, x => x.StartsWith("RestoreVanillaStats is on"));
        }
    }
}
