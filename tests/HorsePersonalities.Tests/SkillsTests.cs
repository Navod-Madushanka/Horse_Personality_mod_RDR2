using System;
using System.Linq;
using HorsePersonalities.Core.Behavior;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Saving;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Skills;
using HorsePersonalities.Core.Stats;
using HorsePersonalities.Core.Tracking;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 8: skills from experience, and long-use growth (design of Nov 28, 2026).</summary>
    public class SkillsTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly SkillTuning T = Catalog.Skills;
        private static Breed B(string n) { Catalog.TryGetBreed(n, out Breed b); return b; }

        // ---- The Skills tab ----

        [Fact]
        public void ShippedSkillsTabLoads()
        {
            Assert.NotNull(T);
            Assert.Equal(3, T.LongUseRank1Bond);
            Assert.Equal(4, T.LongUseRank2Bond);
            Assert.Equal(2, T.FavoriteMaxRanks);
            Assert.True(T.LongUseRank2Hours >= T.LongUseRank1Hours);
        }

        [Theory]
        [InlineData("\"longUseRank2Bond\": 4", "\"longUseRank2Bond\": 2", "must not be below Long use rank 1 bond")]
        [InlineData("\"skillRankAt\": 0.5", "\"skillRankAt\": 0", "skills.skillRankAt")]
        [InlineData("\"favoriteMaxRanks\": 2", "\"favoriteMaxRanks\": 1.5", "must be a whole number")]
        public void BadSkillSettingsAreRejected(string find, string replace, string expected)
        {
            string json = TestData.BreedsJson;
            Assert.Contains(find, json);
            var ex = Assert.Throws<BreedDataException>(() => BreedCatalog.Load(json.Replace(find, replace), new MemoryLog()));
            Assert.Contains(expected, ex.Message);
        }

        // ---- Skills from experience ----

        [Fact]
        public void SkillsGrowWithExperienceAtTheBreedsLearningRate()
        {
            var e = new Experience { Gunfights = 5, GallopMinutes = 10 };
            SkillSet war = SkillSet.From(e, B("Turkoman").Learning, T);     // combat 1.35, physical 1.17
            SkillSet arab = SkillSet.From(e, B("Arabian").Learning, T);     // combat 0.8, physical 1.5
            Assert.True(war.Gunfire > arab.Gunfire);
            Assert.True(arab.Speed > war.Speed);
            Assert.Equal(5 * T.GunfightStep * B("Turkoman").Learning.Combat, war.Gunfire, 9);
        }

        [Fact]
        public void SkillsStopAtOne()
        {
            var e = new Experience { Gunfights = 1000, Predators = 1000, GallopMinutes = 1e6, Sprints = 1e6, RiddenMinutes = 1e6 };
            SkillSet s = SkillSet.From(e, B("Arabian").Learning, T);
            Assert.Equal(1, s.Gunfire);
            Assert.Equal(1, s.Handling);
        }

        [Fact]
        public void TenGunfightsCalmATimidHorseUnderFireButNotAroundBears()
        {
            var timid = new Personality("Morgan", HorseOrigin.Stable, 1, HorseQuality.Bad, new Traits(-2, 0, 0, 0), new[] { Quirk.Nosy },
                new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal);
            double skill = SkillSet.From(new Experience { Gunfights = 10 }, B("Morgan").Learning, T).Gunfire;
            double bonus = skill * T.SkillCourageBonus;
            Assert.True(skill > 0.5, $"10 gunfights should be a real skill, got {skill:0.00}");
            var probe = new BehaviorBrain(Catalog.Behavior, ModSettings.Defaults(), () => 0);
            double untrained = probe.ThreatChance(-2), trained = probe.ThreatChance(-2 + bonus);
            Assert.True(trained < untrained - 0.25, $"spook chance under fire {untrained:0.00} -> {trained:0.00}"); // 0.90 -> 0.61 with the Nov 28 numbers
            // A roll between the two chances: untrained it spooks at gunfire; trained it holds (a tell only).
            // Against a predator, gunfire skill doesn't help: the same roll spooks.
            double roll = (trained + untrained) / 2;
            var brain = new BehaviorBrain(Catalog.Behavior, ModSettings.Defaults(), () => roll);
            Assert.Equal(BehaviorGroup.Spook, Decide(brain, timid, Threat.Gunfire, 0, 0).Group);
            Assert.Equal(BehaviorGroup.Tell, Decide(brain, timid, Threat.Gunfire, bonus, 0).Group);
            Assert.Equal(BehaviorGroup.Spook, Decide(brain, timid, Threat.Predator, bonus, 0).Group);
        }

        private static Decision Decide(BehaviorBrain brain, Personality p, Threat threat, double gunfireCourage, double predatorCourage) =>
            brain.Decide(1, p, new HorseSituation
            {
                Mounted = true, Threat = threat, ThreatEntity = threat == Threat.Predator ? 5 : 0, GameHour = 12, BondLevel = 2,
                GunfireSkillCourage = gunfireCourage, PredatorSkillCourage = predatorCourage,
            }, 0.25, new CooldownBook(Catalog.Behavior, 120), 0);

        // ---- Growth ----

        [Theory]
        [InlineData(1, 0.0, 0, 1, false, 0)]   // a +1 horse hasn't grown into it yet
        [InlineData(1, 0.6, 0, 1, false, 1)]   // ...and has
        [InlineData(-1, 0.0, 0, 1, false, -1)] // a -1 horse shows it from the start
        [InlineData(0, 0.0, 1, 1, false, 1)]   // long use: +1
        [InlineData(1, 1.0, 1, 1, false, 1)]   // any horse stops at its tier's up cap
        [InlineData(-2, 0.0, 1, 1, false, -1)] // long use lifts a slow horse too
        [InlineData(1, 1.0, 2, 1, true, 2)]    // a community favorite: +2, past the tier
        [InlineData(0, 0.0, 2, 1, true, 2)]
        public void SpeedAndAccelerationOffsets(int natural, double skill, int longUse, int tierUp, bool favorite, int expected) =>
            Assert.Equal(expected, GrowthPlanner.Offset(natural, skill, longUse, tierUp, favorite, T));

        [Fact]
        public void LongUseNeedsBothBondAndHours()
        {
            Assert.Equal(0, GrowthPlanner.LongUseRanks(2, 100, false, T));
            Assert.Equal(0, GrowthPlanner.LongUseRanks(4, T.LongUseRank1Hours - 0.1, false, T));
            Assert.Equal(1, GrowthPlanner.LongUseRanks(T.LongUseRank1Bond, T.LongUseRank1Hours, false, T));
            Assert.Equal(1, GrowthPlanner.LongUseRanks(4, 1000, false, T));                  // only favorites get a 2nd
            Assert.Equal(1, GrowthPlanner.LongUseRanks(3, 1000, true, T));                   // a favorite at bond 3
            Assert.Equal(2, GrowthPlanner.LongUseRanks(T.LongUseRank2Bond, T.LongUseRank2Hours, true, T));
        }

        [Fact]
        public void HandlingRaisesAgilityWhenOn()
        {
            Assert.Equal(0, GrowthPlanner.AgilityOffset(0.2, 1, T));
            Assert.Equal(Math.Min(T.HandlingAgilityRanks, 1), GrowthPlanner.AgilityOffset(1.0, 1, T));
        }

        [Fact]
        public void PlanGrowthLearnsOriginalsAndRestores()
        {
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => 7, () => new DateTime(2026, 11, 28));
            HorseRecord r = reg.OnMount(1, new HorseFingerprint("a_c_horse_morgan_bay", true), B("Morgan"), HorseOrigin.Stable, true);
            var speed = new AttributeReading { Attribute = PedAttribute.Speed, BaseRank = 4, MaxRank = 9 };
            var changes = StatPlanner.PlanGrowth(r, new[] { (StatPlanner.Speed, speed, 1, 1) }, false, out var learned);
            Assert.Equal(("speed", 4), learned.Single());
            Assert.Equal(5, changes.Single().To);
            reg.RememberVanillaBase(r, "speed", 4);
            speed.BaseRank = 5;
            var back = StatPlanner.PlanGrowth(r, new[] { (StatPlanner.Speed, speed, 1, 1) }, true, out _);
            Assert.Equal(4, back.Single().To);
            speed.BaseRank = 4;
            Assert.Equal(9, StatPlanner.PlanGrowth(r, new[] { (StatPlanner.Speed, speed, 7, 1) }, false, out _).Single().To); // never past the game's max
        }

        // ---- Saving ----

        [Fact]
        public void ExperienceSurvivesTheSaveAndOldFilesLoadWithNone()
        {
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => 9, () => new DateTime(2026, 11, 28));
            HorseRecord r = reg.OnMount(1, new HorseFingerprint("a_c_horse_morgan_bay", true), B("Morgan"), HorseOrigin.Stable, true);
            r.Experience.Gunfights = 3;
            r.Experience.RiddenMinutes = 412.5;
            r.Experience.Sprints = 37;
            string text = HorseSaveFormat.Write(reg.Permanent);
            var back = HorseSaveFormat.Read(text, new MemoryLog()).Single();
            Assert.Equal(3, back.Experience.Gunfights);
            Assert.Equal(412.5, back.Experience.RiddenMinutes);
            Assert.Equal(37, back.Experience.Sprints);

            string old = System.Text.RegularExpressions.Regex.Replace(text, "\"experience\":\\s*\\{[^}]*\\},?", "");
            Assert.DoesNotContain("experience", old);
            Assert.True(HorseSaveFormat.Read(old, new MemoryLog()).Single().Experience.IsEmpty);
        }

        [Fact]
        public void TestPresetsKeepTheHorsesExperience()
        {
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => 9, () => new DateTime(2026, 11, 28));
            var id = new HorseFingerprint("a_c_horse_morgan_bay", true);
            HorseRecord r = reg.OnMount(1, id, B("Morgan"), HorseOrigin.Stable, true);
            r.Experience.Gunfights = 4;
            HorseRecord replaced = reg.Replace(1, id, r.Personality, "Average");
            Assert.Equal(4, replaced.Experience.Gunfights);
        }

        // ---- Overlay ----

        [Fact]
        public void OverlayShowsSkillsForAPermanentHorse()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_horse_morgan_bay"), bondRank: 3);
            var d = HorseDescriber.Describe(game, Catalog, 10);
            var reg = new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => 9, () => new DateTime(2026, 11, 28));
            HorseRecord r = reg.OnMount(10, new HorseFingerprint(d.ModelName, d.IsMale), d.Breed, HorseOrigin.Stable, true);
            r.Experience.RiddenMinutes = T.LongUseRank1Hours * 60;
            var lines = OverlayText.Build(d, r, T, false);
            Assert.Contains(lines, l => l.StartsWith("Skills: "));
            Assert.Contains("Long-use growth: 1 rank", lines);
        }
    }
}
