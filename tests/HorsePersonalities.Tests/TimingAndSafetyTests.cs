using System;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Safety;
using HorsePersonalities.Core.Timing;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class TimingAndSafetyTests
    {
        [Fact]
        public void IntervalGateFiresOncePerPeriod()
        {
            var g = new IntervalGate(250);
            Assert.True(g.Due(1000));
            Assert.False(g.Due(1100));
            Assert.False(g.Due(1249));
            Assert.True(g.Due(1250));
            Assert.False(g.Due(1251));
        }

        [Fact]
        public void IntervalGateSurvivesTimerWraparound()
        {
            var g = new IntervalGate(1000);
            Assert.True(g.Due(int.MaxValue - 100));
            Assert.False(g.Due(unchecked(int.MaxValue + 500)));
            Assert.True(g.Due(unchecked(int.MaxValue + 1000)));
        }

        [Fact]
        public void CooldownBlocksUntilItEnds()
        {
            var c = new Cooldown(120_000);
            Assert.True(c.TryStart(0));
            Assert.False(c.TryStart(60_000));
            Assert.Equal(60_000, c.RemainingMs(60_000));
            Assert.True(c.IsReady(120_000));
            Assert.True(c.TryStart(120_000));
        }

        [Fact]
        public void GuardTurnsOffOnlyTheFailingFeature()
        {
            var log = new MemoryLog();
            var guard = new FeatureGuard(log, maxFailures: 3);
            int healthyRuns = 0;
            for (int i = 0; i < 5; i++)
            {
                guard.Run("Spooks", () => throw new InvalidOperationException("native missing"));
                guard.Run("Quirks", () => healthyRuns++);
            }
            Assert.False(guard.IsEnabled("Spooks"));
            Assert.True(guard.IsEnabled("Quirks"));
            Assert.Equal(5, healthyRuns);
            Assert.Equal(4, log.Entries.FindAll(e => e.Category == "Spooks").Count); // 3 failures + 1 "turned off"
        }

        [Fact]
        public void GuardReturnsFallbackOnFailure()
        {
            var guard = new FeatureGuard(new MemoryLog());
            Assert.Equal(-1, guard.Run<int>("Read", () => throw new Exception("x"), -1));
            Assert.Equal(4, guard.Run("Other", () => 4, -1));
        }
    }
}
