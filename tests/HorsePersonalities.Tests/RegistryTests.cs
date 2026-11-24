using System;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 2: temporary and permanent personalities, and the fingerprint.</summary>
    public class RegistryTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly Breed Turkoman = Get("Turkoman");
        private static readonly Breed Arabian = Get("Arabian");
        private static readonly HorseFingerprint Gold = new HorseFingerprint("a_c_horse_turkoman_gold", true);
        private static readonly HorseFingerprint Silver = new HorseFingerprint("a_c_horse_turkoman_silver", true);

        private static Breed Get(string n) { Catalog.TryGetBreed(n, out Breed b); return b; }

        private static HorseRegistry NewRegistry(MemoryLog log = null)
        {
            ulong seed = 1000;
            return new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), log ?? new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 24));
        }

        [Fact]
        public void FingerprintKeyRoundTrips()
        {
            Assert.Equal("a_c_horse_turkoman_gold|male|default", Gold.Key);
            Assert.True(HorseFingerprint.TryParse("A_C_Horse_Arabian_White|female|default", out var f));
            Assert.Equal("a_c_horse_arabian_white|female|default", f.Key);
            Assert.False(HorseFingerprint.TryParse("a_c_horse_arabian_white|mare|default", out _));
            Assert.False(HorseFingerprint.TryParse("no-pipes", out _));
            Assert.Throws<ArgumentException>(() => new HorseFingerprint("a|b", true));
        }

        [Fact]
        public void TwoCoatsOfOneBreedGetDifferentIds()
        {
            Assert.NotEqual(Gold.Key, Silver.Key);
            Assert.NotEqual(new HorseFingerprint("a_c_horse_turkoman_gold", false).Key, Gold.Key);
        }

        [Fact]
        public void MountingSomeoneElsesHorseGivesATemporaryPersonality()
        {
            var reg = NewRegistry();
            var r = reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, isSaddleHorse: false);
            Assert.Equal(PersonalityStatus.Temporary, r.Status);
            Assert.Empty(reg.Permanent);
            Assert.False(reg.Dirty); // nothing to save
            Assert.Same(r, reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false)); // same horse, same personality
        }

        [Fact]
        public void SaddlingMakesTheSamePersonalityPermanent()
        {
            var reg = NewRegistry();
            var temp = reg.OnMount(10, Gold, Turkoman, HorseOrigin.Wild, false);
            var kept = reg.OnSaddle(10, Gold, Turkoman);
            Assert.Equal(PersonalityStatus.Permanent, kept.Status);
            Assert.Same(temp.Personality, kept.Personality);
            Assert.Equal(HorseOrigin.Wild, kept.Personality.Origin); // a broken wild horse stays wild
            Assert.True(reg.Dirty);
            Assert.Equal(0, reg.TemporaryCount);
        }

        [Fact]
        public void FirstSightOfTheSaddleHorseRollsItOnceAsAStableHorse()
        {
            var reg = NewRegistry();
            var first = reg.OnSaddle(5, Gold, Turkoman);
            Assert.Equal(HorseOrigin.Stable, first.Personality.Origin);
            Assert.Equal(BreakingDifficulty.Normal, first.Personality.Breaking);
            Assert.Same(first, reg.OnSaddle(5, Gold, Turkoman));
            Assert.Same(first, reg.OnMount(5, Gold, Turkoman, HorseOrigin.Unknown, isSaddleHorse: true));
            Assert.Single(reg.Permanent);
        }

        [Fact]
        public void MountingTheSaddleHorseCountsAsSaddling()
        {
            var reg = NewRegistry();
            Assert.Equal(PersonalityStatus.Permanent, reg.OnMount(5, Gold, Turkoman, HorseOrigin.Unknown, isSaddleHorse: true).Status);
        }

        [Fact]
        public void TemporaryPersonalityIsGoneAfterAReload()
        {
            var before = NewRegistry();
            before.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false);
            var after = NewRegistry(); // a reload: only permanent records come back
            after.Load(before.Permanent);
            Assert.Null(after.Find(10, Gold));
        }

        [Fact]
        public void PermanentHorseComesBackAfterAReloadWithANewHandle()
        {
            var before = NewRegistry();
            var kept = before.OnSaddle(10, Gold, Turkoman);
            var after = NewRegistry();
            after.Load(before.Permanent);
            Assert.False(after.Dirty);
            var again = after.OnMount(77, Gold, Turkoman, HorseOrigin.Unknown, false); // handles change across reloads
            Assert.Same(kept.Personality, again.Personality);
        }

        [Fact]
        public void HorsesWithNoBreedStayVanilla()
        {
            var reg = NewRegistry();
            Assert.Null(reg.OnMount(10, new HorseFingerprint("a_c_horsemule_01", true), null, HorseOrigin.Unknown, false));
            Assert.Null(reg.OnSaddle(10, new HorseFingerprint("a_c_horsemule_01", true), null));
            Assert.Empty(reg.Permanent);
        }

        [Fact]
        public void AReusedHandleWithAnotherHorseRollsAgain()
        {
            var reg = NewRegistry();
            var a = reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false);
            var b = reg.OnMount(10, new HorseFingerprint("a_c_horse_arabian_white", false), Arabian, HorseOrigin.Unknown, false);
            Assert.NotSame(a, b);
            Assert.Equal("Arabian", b.Personality.Breed);
        }

        [Fact]
        public void TwoLiveHorsesWithOneFingerprintAreWarnedAboutOnce()
        {
            var log = new MemoryLog();
            var reg = NewRegistry(log);
            reg.OnSaddle(10, Gold, Turkoman);
            reg.OnMount(11, Gold, Turkoman, HorseOrigin.Unknown, false);
            reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false);
            Assert.Single(log.Entries, e => e.Level == LogLevel.Warn && e.Message.Contains("share fingerprint"));
        }

        [Fact]
        public void SeeingAKeptHorseOnANewDayUpdatesLastSeen()
        {
            var day = new DateTime(2026, 11, 24);
            ulong seed = 1;
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => seed++, () => day);
            reg.OnSaddle(10, Gold, Turkoman);
            reg.MarkSaved();
            reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false);
            Assert.False(reg.Dirty);
            day = day.AddDays(1);
            var r = reg.OnMount(10, Gold, Turkoman, HorseOrigin.Unknown, false);
            Assert.True(reg.Dirty);
            Assert.Equal("2026-11-25", r.LastSeen);
            Assert.Equal("2026-11-24", r.FirstSeen);
        }
    }
}
