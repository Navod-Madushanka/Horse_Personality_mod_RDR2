using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Behavior;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 6 (decisions of Nov 26, 2026).</summary>
    public class BehaviorTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly BehaviorTuning T = Catalog.Behavior;

        private static Personality Horse(double courage = 0, double nerves = 0, double temper = 0, double bond = 0,
            HorseQuality q = HorseQuality.Good, bool prized = false, BreakingDifficulty breaking = BreakingDifficulty.Normal) =>
            new Personality("Morgan", HorseOrigin.Stable, 1, q, new Traits(courage, nerves, temper, bond), new[] { Quirk.Shadow },
                new NaturalStats(0, 0, 0, 0), breaking, prized);

        private static BehaviorBrain Brain(Func<double> rng, double spookFrequency = 1.0) =>
            new BehaviorBrain(T, ModSettings.Load($"SpookFrequency = {spookFrequency.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n", new MemoryLog()), rng);

        private static HorseSituation Riding(double speed = 3, int hour = 12, int bond = 2) =>
            new HorseSituation { Mounted = true, SpeedMs = speed, GameHour = hour, BondLevel = bond };

        // ---- Strength: "noticeable but rare" ----

        [Fact]
        public void AVeryNervousHorseSpooksAboutEvery10To15MinutesAtNight()
        {
            double perMinute = Brain(() => 1).RandomSpookRate(2.0, Riding(hour: 23), out _);
            Assert.InRange(1 / perMinute, 10, 15);
        }

        [Fact]
        public void AnAverageHorseRarelySpooksAtNothing()
        {
            var b = Brain(() => 1);
            Assert.True(1 / b.RandomSpookRate(0, Riding(hour: 12), out _) >= 60, "average horse in daylight: at most once an hour");
            Assert.True(1 / b.RandomSpookRate(0, Riding(hour: 23), out _) >= 30, "average horse at night: at most twice an hour");
        }

        [Fact]
        public void NightGallopAndWeakBondMakeSpooksMoreLikely_FullBondLess()
        {
            var b = Brain(() => 1);
            double day = b.RandomSpookRate(1, Riding(), out _);
            Assert.True(b.RandomSpookRate(1, Riding(hour: 22), out string f) > day && f.Contains("night"));
            Assert.True(b.RandomSpookRate(1, Riding(speed: 12), out f) > day && f.Contains("gallop"));
            Assert.True(b.RandomSpookRate(1, Riding(bond: 1), out f) > day && f.Contains("weak bond"));
            Assert.True(b.RandomSpookRate(1, Riding(bond: 4), out f) < day && f.Contains("full bond"));
        }

        [Fact]
        public void SpookFrequencyScalesRandomSpooks()
        {
            Assert.Equal(0, Brain(() => 1, 0).RandomSpookRate(2, Riding(hour: 23), out _));
            Assert.Equal(2 * Brain(() => 1).RandomSpookRate(1, Riding(), out _), Brain(() => 1, 2).RandomSpookRate(1, Riding(), out _), 9);
        }

        [Fact]
        public void ACowardReactsToMostThreatsABraveHorseRarely()
        {
            var b = Brain(() => 1);
            Assert.InRange(b.ThreatChance(-2), 0.85, 0.95);
            Assert.InRange(b.ThreatChance(2), 0.05, 0.15);
            Assert.Equal(T.ThreatSpookChance, b.ThreatChance(0), 9);
        }

        [Theory]
        [InlineData(5, 23)] [InlineData(21, 23)] [InlineData(23, 23)] [InlineData(0, 23)] [InlineData(4, 23)]
        public void NightRunsFrom21To5(int hour, int _)
        {
            Assert.True(Brain(() => 1).IsNight(hour) == (hour >= 21 || hour < 5));
        }

        // ---- Threat reactions ----

        private static Decision Scare(Personality p, bool mounted, Threat threat = Threat.Predator, double roll = 0.0, CooldownBook cd = null)
        {
            var s = mounted ? Riding() : new HorseSituation { PlayerNear = true, GameHour = 12, BondLevel = 2 };
            s.Threat = threat;
            s.ThreatEntity = 99;
            return Brain(() => roll).Decide(1, p, s, 0.25, cd ?? new CooldownBook(T), 1000);
        }

        [Fact]
        public void ARiddenCowardRears() => Assert.Equal(BehaviorAction.Rear, Scare(Horse(courage: -1.5), true).Action);

        [Fact]
        public void ARiddenHotheadThrowsYouInsteadOfRearing() => Assert.Equal(BehaviorAction.ThrowRider, Scare(Horse(courage: -1.5, temper: 1.5), true).Action);

        [Fact]
        public void ABraveButSpookedHorseOnlyGetsAgitated() => Assert.Equal(BehaviorAction.Agitate, Scare(Horse(courage: 1), true).Action);

        [Fact]
        public void ACowardYouAreNotRidingBolts() => Assert.Equal(BehaviorAction.Bolt, Scare(Horse(courage: -1), false).Action);

        [Fact]
        public void ABraverHorseYouAreNotRidingWatchesTheThreat() => Assert.Equal(BehaviorAction.LookAtThreat, Scare(Horse(courage: 0.5), false).Action);

        [Fact]
        public void ATimidHorseThatDoesNotSpookStillGivesATell()
        {
            var d = Scare(Horse(courage: -0.5), true, roll: 0.999);
            Assert.Equal(BehaviorAction.LookAtThreat, d.Action);
            Assert.Equal(BehaviorGroup.Tell, d.Group);
            Assert.Contains("tell", d.Cause);
        }

        [Fact]
        public void ABraveHorseThatDoesNotSpookDoesNothing() => Assert.True(Scare(Horse(courage: 1), true, roll: 0.999).IsNone);

        [Fact]
        public void ExplosionsScareMoreThanCourageAlone()
        {
            // Courage +0.5, roll 0.4: a predator (chance ~0.37) doesn't scare it, an explosion (as Courage -0.5, ~0.63) does.
            Assert.True(Scare(Horse(courage: 0.5), true, Threat.Predator, 0.4).IsNone);
            Assert.False(Scare(Horse(courage: 0.5), true, Threat.Explosion, 0.4).IsNone);
        }

        [Fact]
        public void YourOwnGunfireScaresLessThanAStrangers()
        {
            // Courage -1 (chance ~0.75): roll 0.5 spooks at a stranger's shot, not at yours (x0.4 = ~0.30).
            Assert.False(Scare(Horse(courage: -1), true, Threat.Gunfire, 0.5).IsNone);
            Assert.NotEqual(BehaviorGroup.Spook, Scare(Horse(courage: -1), true, Threat.RiderGunfire, 0.5).Group);
        }

        [Fact]
        public void TheCauseNamesTheTraitAndTheRoll()
        {
            var d = Scare(Horse(courage: -1.5), true);
            Assert.Contains("Courage -1.50", d.Cause);
            Assert.Contains("roll 0.000 vs", d.Cause);
        }

        // ---- Cooldowns ----

        [Fact]
        public void ASpookStartsItsCooldownAndTheGap()
        {
            var cd = new CooldownBook(T);
            Assert.False(Scare(Horse(courage: -2), true, cd: cd).IsNone);
            Assert.False(cd.Ready(1, BehaviorGroup.Spook, 1000 + T.SpookCooldown - 1));
            Assert.True(cd.Ready(1, BehaviorGroup.Spook, 1000 + T.SpookCooldown));
            Assert.False(cd.Ready(1, BehaviorGroup.Buck, 1000 + T.BehaviorGap - 1)); // the shortest gap between any two
            Assert.True(cd.Ready(1, BehaviorGroup.Buck, 1000 + T.BehaviorGap));
            Assert.True(cd.Ready(2, BehaviorGroup.Spook, 1000)); // per horse
        }

        [Fact]
        public void NoBehaviorDuringItsCooldown()
        {
            var cd = new CooldownBook(T);
            Scare(Horse(courage: -2), true, cd: cd);
            var again = Scare(Horse(courage: -2), true, cd: cd);
            Assert.NotEqual(BehaviorGroup.Spook, again.Group);
        }

        // ---- Pushed ----

        [Fact]
        public void PushedHotheadThrowsYouAnAverageHorseRefuses()
        {
            var s = Riding(speed: 10);
            s.Pushed = PushKind.LongSprint;
            Assert.Equal(BehaviorAction.ThrowRider, Brain(() => 0).Decide(1, Horse(temper: 1.5), s, 0.25, new CooldownBook(T), 0).Action);
            Assert.Equal(BehaviorAction.QuickStop, Brain(() => 0).Decide(1, Horse(temper: 0), s, 0.25, new CooldownBook(T), 0).Action);
        }

        [Fact]
        public void PushBuckChanceGrowsWithTemperUpToTheMax()
        {
            var s = Riding(speed: 10);
            s.Pushed = PushKind.HardStop;
            bool Bucks(double temper, double roll) => !Brain(() => roll).Decide(1, Horse(temper: temper), s, 0.25, new CooldownBook(T), 0).IsNone;
            Assert.True(Bucks(0, 0.049) && !Bucks(0, 0.051)); // 5% at Temper 0
            Assert.True(Bucks(2, 0.36) && !Bucks(2, 0.38));   // ~37% at Temper +2
            Assert.False(Bucks(5, 0.51));                     // never above 50%
        }

        [Fact]
        public void NotPushedNoBuck()
        {
            Assert.True(Brain(() => 0.5).Decide(1, Horse(temper: 2), Riding(), 0.25, new CooldownBook(T), 0).IsNone);
        }

        [Fact]
        public void PushDetectorReportsEachPushOnce()
        {
            var push = new PushDetector(T);
            Assert.Equal(PushKind.LowStaminaSpur, push.Update(0.25, true, 5, 10, true));
            Assert.Equal(PushKind.None, push.Update(0.25, true, 5, 10, true)); // still spurring: not again
            push.Update(0.25, true, 5, 10, false);
            Assert.Equal(PushKind.LowStaminaSpur, push.Update(0.25, true, 5, 10, true)); // a new spur

            push = new PushDetector(T);
            PushKind seen = PushKind.None;
            for (double t = 0; t < T.LongSprintSeconds + 1; t += 0.25) seen |= push.Update(0.25, true, 12, 80, true);
            Assert.Equal(PushKind.LongSprint, seen);

            push = new PushDetector(T);
            push.Update(0.25, true, 12, 80, false);
            Assert.Equal(PushKind.HardStop, push.Update(0.25, true, 9, 80, false)); // lost 3 m/s in 0.25 s = 12 m/s per s
            Assert.Equal(PushKind.None, new PushDetector(T).Update(0.25, false, 12, 0, true)); // not ridden: nothing
        }

        // ---- Random spooks ----

        [Fact]
        public void RandomSpookReactionsDependOnSpeedAndCourage()
        {
            Decision D(Personality p, HorseSituation s) => Brain(() => 0).Decide(1, p, s, 0.25, new CooldownBook(T), 0);
            Assert.Equal(BehaviorAction.Skid, D(Horse(nerves: 2), Riding(speed: 12)).Action);
            Assert.Equal(BehaviorAction.Rear, D(Horse(nerves: 2, courage: -1), Riding()).Action);
            Assert.Equal(BehaviorAction.Agitate, D(Horse(nerves: 2, courage: 1), Riding()).Action);
            Assert.Equal(BehaviorAction.Agitate, D(Horse(nerves: 2), new HorseSituation { PlayerNear = true, GameHour = 12, BondLevel = 2 }).Action);
        }

        [Fact]
        public void OverAnHourOfNightRidingAVeryNervousHorseSpooksRoughly4To6Times()
        {
            var rng = new Random(7);
            var brain = new BehaviorBrain(T, ModSettings.Defaults(), () => rng.NextDouble());
            var cd = new CooldownBook(T);
            int spooks = 0;
            for (double now = 0; now < 3600; now += 0.25)
                if (!brain.Decide(1, Horse(nerves: 2, courage: 1), Riding(hour: 23), 0.25, cd, now).IsNone) spooks++;
            Assert.InRange(spooks, 2, 9);
        }

        [Fact]
        public void NothingHappensToAHorseYouAreNeitherRidingNorNear()
        {
            var s = new HorseSituation { GameHour = 23, Threat = Threat.Explosion };
            Assert.True(Brain(() => 0).Decide(1, Horse(courage: -2, nerves: 2), s, 0.25, new CooldownBook(T), 0).IsNone);
        }

        // ---- Kick ----

        [Fact]
        public void LingeringBehindAHotheadGetsYouKicked()
        {
            var s = new HorseSituation { PlayerNear = true, GameHour = 12, BondLevel = 2, PlayerLingeredBehind = true };
            Assert.Equal(BehaviorAction.Kick, Brain(() => 0.3).Decide(1, Horse(temper: 2), s, 0.25, new CooldownBook(T), 0).Action);
            Assert.True(Brain(() => 0.3).Decide(1, Horse(temper: 0), s, 0.25, new CooldownBook(T), 0).IsNone);
        }

        [Fact]
        public void LingerTrackerNeedsYouBehindForTheWholeTime()
        {
            var l = new LingerTracker(T);
            bool fired = false;
            for (double t = 0; t < T.KickLingerSeconds - 0.25; t += 0.25) fired |= l.Update(0.25, false, 1.5, 0.2);
            Assert.False(fired);
            Assert.True(l.Update(0.5, false, 1.5, 0.2));
            Assert.False(l.Update(0.25, false, 1.5, 0.2)); // once per episode
            Assert.False(l.Update(0.25, false, -1.5, 0)); // in front: episode over
            Assert.False(new LingerTracker(T).Update(10, false, 1.5, 3)); // off to the side
        }

        [Fact]
        public void BehindAndSideFromPositions()
        {
            var (behind, side) = Vec3.BehindAndSide(new Vec3(0, 0, 0), new Vec3(0, 1, 0), new Vec3(0.5f, -2, 0));
            Assert.Equal(2, behind, 3);
            Assert.Equal(0.5, Math.Abs(side), 3);
        }

        // ---- Threat memory ----

        [Fact]
        public void AThreatIsJudgedOnceUntilItGoesQuiet()
        {
            var m = new ThreatMemory(T);
            Assert.True(m.IsNew(1, "predator 5", 0));
            for (double t = 0.5; t < 60; t += 0.5) Assert.False(m.IsNew(1, "predator 5", t)); // the wolf stays: no re-roll
            Assert.True(m.IsNew(1, "predator 6", 60));  // a different wolf
            Assert.True(m.IsNew(2, "predator 5", 60));  // a different horse
            Assert.True(m.IsNew(1, "gunfire", 0));
            Assert.False(m.IsNew(1, "gunfire", 10));
            Assert.True(m.IsNew(1, "gunfire", 10 + T.ThreatQuietSeconds + 1)); // a new gunfight
        }

        // ---- Mission guard ----

        [Fact]
        public void MissionsAndCutscenesHoldBehaviors()
        {
            var calm = new GuardSignals { GameplayCameraRendering = true, PlayerControlOn = true };
            Assert.Null(MissionGuard.Holds(calm, true));
            var m = calm; m.MissionFlag = true; Assert.Equal("story mission", MissionGuard.Holds(m, true));
            var l = calm; l.LetterBox = true; Assert.Contains("letterbox", MissionGuard.Holds(l, true));
            var a = calm; a.AnimSceneCamera = true; Assert.Contains("anim scene", MissionGuard.Holds(a, true));
            var g = calm; g.GameplayCameraRendering = false; Assert.Contains("gameplay camera", MissionGuard.Holds(g, true));
            var c = calm; c.PlayerControlOn = false; Assert.Contains("player control", MissionGuard.Holds(c, true));
            Assert.Null(MissionGuard.Holds(m, false)); // DisableDuringMissions = false
        }

        // ---- Bond pace ----

        [Fact]
        public void BondAffinitySpeedsUpOrSlowsBonding()
        {
            var pace = new BondPace(T, Catalog.Roller);
            Assert.InRange(pace.Multiplier(Horse(bond: 2), 2), 1.45, 1.55);
            Assert.InRange(pace.Multiplier(Horse(bond: -2), 2), 0.65, 0.69);
            Assert.Equal(1.0, pace.Multiplier(Horse(bond: 0, q: HorseQuality.Bad), 3), 9);
        }

        [Fact]
        public void TheClimbToBond4IsSlowerForGoodAndSlowestForPrized()
        {
            var pace = new BondPace(T, Catalog.Roller);
            Assert.Equal(1.0, pace.Multiplier(Horse(prized: true), 2), 9);      // levels 1-3: normal
            Assert.Equal(0.75, pace.Multiplier(Horse(), 3), 9);                  // good, bond 3 -> 4
            Assert.Equal(0.5, pace.Multiplier(Horse(prized: true), 3), 9);       // prized, bond 3 -> 4
            Assert.Equal(1.0, pace.Multiplier(Horse(q: HorseQuality.Bad), 3), 9); // bad: normal
        }

        [Fact]
        public void HardWildHorsesBondSlower() =>
            Assert.Equal(0.8, new BondPace(T, Catalog.Roller).Multiplier(Horse(breaking: BreakingDifficulty.Hard), 2), 9);

        // ---- Trust (Build plan: Trust) ----

        private static readonly int[] Scale = { 350, 950, 1950 }; // the Dutch Warmblood's thresholds, read in game

        private static Core.Identity.HorseRecord Kept(Personality p, Core.Identity.PersonalityStatus status = Core.Identity.PersonalityStatus.Permanent) =>
            new Core.Identity.HorseRecord(new Core.Identity.HorseFingerprint("a_c_horse_morgan_bay", true), p, status, "2026-11-28", "2026-11-28");

        [Fact]
        public void TrustLevelsFollowTheGameThresholds()
        {
            Assert.Equal(1, TrustLevels.Level(0, Scale));
            Assert.Equal(1, TrustLevels.Level(349.9, Scale));
            Assert.Equal(2, TrustLevels.Level(350, Scale));
            Assert.Equal(3, TrustLevels.Level(1949, Scale));
            Assert.Equal(4, TrustLevels.Level(5000, Scale));
            Assert.NotNull(TrustLevels.Valid(350, 950, 1950));
            Assert.Null(TrustLevels.Valid(0, 0, 0));
            Assert.Null(TrustLevels.Valid(350, 350, 1950));
        }

        [Fact]
        public void TrustStartsAtTheGameBondAndKeepsIt()
        {
            var tracker = new TrustTracker(new BondPace(T, Catalog.Roller));
            var r = Kept(Horse(q: HorseQuality.Bad));
            TrustUpdate u = tracker.Update(1, r, 960, Scale);
            Assert.Equal(TrustUpdateKind.Started, u.Kind);
            Assert.True(u.Save);
            Assert.Equal(960, r.Trust);
            Assert.Equal(3, u.Level);
        }

        [Fact]
        public void TrustScalesEachGainAndNeverWritesTheGame()
        {
            var tracker = new TrustTracker(new BondPace(T, Catalog.Roller));
            var r = Kept(Horse(prized: true)); // x0.5 from level 3 to 4
            tracker.Update(1, r, 1000, Scale);
            TrustUpdate u = tracker.Update(1, r, 1010, Scale);
            Assert.Equal(TrustUpdateKind.Gained, u.Kind);
            Assert.Equal(1005, r.Trust.Value, 9);
            Assert.False(u.Save); // 5 points: not worth a write yet
        }

        [Fact]
        public void TrustIgnoresTheGamePuttingItsXpBack()
        {
            var tracker = new TrustTracker(new BondPace(T, Catalog.Roller));
            var r = Kept(Horse(q: HorseQuality.Bad));
            tracker.Update(1, r, 500, Scale);
            tracker.Update(1, r, 520, Scale);                      // +20
            TrustUpdate drop = tracker.Update(1, r, 86, Scale);   // the game restores its saved value
            Assert.Equal(TrustUpdateKind.GameRestored, drop.Kind);
            Assert.Equal(520, r.Trust.Value, 9);
            tracker.Update(1, r, 96, Scale);                       // counting goes on from the restored value
            Assert.Equal(530, r.Trust.Value, 9);
        }

        [Fact]
        public void ANewSessionStartsFromTheGameValueWithoutCountingTheDifference()
        {
            var r = Kept(Horse(q: HorseQuality.Bad));
            new TrustTracker(new BondPace(T, Catalog.Roller)).Update(1, r, 500, Scale);
            var after = new TrustTracker(new BondPace(T, Catalog.Roller)); // a reload: new tracker, new handle
            Assert.Equal(TrustUpdateKind.None, after.Update(7, r, 86, Scale).Kind);
            Assert.Equal(500, r.Trust.Value, 9);
        }

        [Fact]
        public void AnOutsideJumpCountsAsItIs()
        {
            var tracker = new TrustTracker(new BondPace(T, Catalog.Roller));
            var r = Kept(Horse(prized: true, bond: -2));
            tracker.Update(1, r, 86, Scale);
            TrustUpdate u = tracker.Update(1, r, 350, Scale); // DevTools trigger 42: level 2
            Assert.Equal(TrustUpdateKind.FromOutside, u.Kind);
            Assert.Equal(350, r.Trust.Value, 9);
            Assert.Equal(2, u.Level);
            Assert.True(u.Save);
        }

        [Fact]
        public void ALevelUpIsSavedAndTemporaryHorsesHaveNoTrust()
        {
            var tracker = new TrustTracker(new BondPace(T, Catalog.Roller));
            var r = Kept(Horse(q: HorseQuality.Bad));
            tracker.Update(1, r, 345, Scale);
            TrustUpdate u = tracker.Update(1, r, 352, Scale);
            Assert.Equal((1, 2), (u.LevelBefore, u.Level));
            Assert.True(u.Save);

            var temp = Kept(Horse(), Core.Identity.PersonalityStatus.Temporary);
            Assert.Equal(TrustUpdateKind.None, tracker.Update(2, temp, 500, Scale).Kind);
            Assert.Null(temp.Trust);
        }

        // ---- Breaking ----

        [Fact]
        public void AnUnbreakableHorseThrowsYouEveryAttempt()
        {
            var b = new BreakingDirector(T);
            for (int attempt = 0; attempt < 5; attempt++)
            {
                bool thrown = false;
                for (double t = 0; t < 30; t += 0.25) thrown |= b.Update(BreakingDifficulty.Unbreakable, true, 0.25, out _);
                Assert.True(thrown, $"attempt {attempt + 1}");
                b.Update(BreakingDifficulty.Unbreakable, false, 0.25, out _); // thrown off: the attempt ends
            }
        }

        [Fact]
        public void AHardHorseThrowsYouAFewExtraTimesThenCanBreak()
        {
            var b = new BreakingDirector(T);
            int throws = 0;
            for (int attempt = 0; attempt < T.HardExtraThrows + 2; attempt++)
            {
                for (double t = 0; t < 30; t += 0.25) if (b.Update(BreakingDifficulty.Hard, true, 0.25, out string why)) { throws++; Assert.Contains("Hard", why); }
                b.Update(BreakingDifficulty.Hard, false, 0.25, out _);
            }
            Assert.Equal(T.HardExtraThrows, throws);
        }

        [Fact]
        public void ANormalHorseIsLeftToTheGame()
        {
            var b = new BreakingDirector(T);
            for (double t = 0; t < 60; t += 0.25) Assert.False(b.Update(BreakingDifficulty.Normal, true, 0.25, out _));
        }
    }
}
