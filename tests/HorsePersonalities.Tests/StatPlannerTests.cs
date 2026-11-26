using System;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Saving;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Stats;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 5: health and stamina differences on the stat bars as the bond grows.</summary>
    public class StatPlannerTests
    {
        [Theory]
        // natural, bond -> shown (first rank at bond 2, second at bond 3)
        [InlineData(1, 1, 0)]
        [InlineData(1, 2, 1)]
        [InlineData(1, 4, 1)]
        [InlineData(-1, 1, 0)]
        [InlineData(-1, 2, -1)]
        [InlineData(-2, 2, -1)]
        [InlineData(-2, 3, -2)]
        [InlineData(-2, 4, -2)]
        [InlineData(0, 4, 0)]
        public void RevealsTheFirstRankAtBond2AndTheSecondAtBond3(int natural, int bond, int shown) =>
            Assert.Equal(shown, StatPlanner.Revealed(natural, bond, 2, 3));

        private static HorseRecord Record(int health, int stamina, PersonalityStatus status = PersonalityStatus.Permanent)
        {
            var p = new Personality("Morgan", HorseOrigin.Stable, 1, HorseQuality.Good, new Traits(0, 0, 0, 0),
                new[] { Quirk.Shadow }, new NaturalStats(0, 0, health, stamina), BreakingDifficulty.Normal, true);
            return new HorseRecord(new HorseFingerprint("a_c_horse_morgan_bay", true), p, status, "2026-11-26", "2026-11-26");
        }

        private static AttributeReading R(PedAttribute a, int baseRank, int bonus = 0, int max = 9) =>
            new AttributeReading { Attribute = a, BaseRank = baseRank, BonusRank = bonus, Rank = baseRank + bonus, MaxRank = max };

        [Fact]
        public void Bond1ShowsVanillaBarsAndLearnsTheOriginals()
        {
            var rec = Record(1, -1);
            var changes = StatPlanner.Plan(rec, 1, R(PedAttribute.Health, 4), R(PedAttribute.Stamina, 5), 2, 3, false, out var learned);
            Assert.Empty(changes);
            Assert.Equal(new[] { ("health", 4), ("stamina", 5) }, learned.ToArray());
        }

        [Fact]
        public void Bond2MovesEachStatOneRankFromItsOriginal()
        {
            var rec = Record(1, -2);
            rec.VanillaBase["health"] = 4;
            rec.VanillaBase["stamina"] = 5;
            var changes = StatPlanner.Plan(rec, 2, R(PedAttribute.Health, 4), R(PedAttribute.Stamina, 5), 2, 3, false, out var learned);
            Assert.Empty(learned);
            Assert.Equal(2, changes.Count);
            Assert.Equal(5, changes.Single(c => c.Attribute == PedAttribute.Health).To);
            Assert.Equal(4, changes.Single(c => c.Attribute == PedAttribute.Stamina).To);
            // Bond 3: the -2 horse drops its second rank.
            var bond3 = StatPlanner.Plan(rec, 3, R(PedAttribute.Health, 5), R(PedAttribute.Stamina, 4), 2, 3, false, out _);
            Assert.Equal(3, bond3.Single().To);
        }

        [Fact]
        public void AStatAlreadyAtItsTargetIsLeftAlone()
        {
            var rec = Record(1, 0);
            rec.VanillaBase["health"] = 4;
            rec.VanillaBase["stamina"] = 5;
            Assert.Empty(StatPlanner.Plan(rec, 4, R(PedAttribute.Health, 5), R(PedAttribute.Stamina, 5), 2, 3, false, out _));
        }

        [Fact]
        public void AGameResetIsPutBack()
        {
            // The game reloaded the horse with its own base rank: the mod sets it again.
            var rec = Record(1, 0);
            rec.VanillaBase["health"] = 4;
            rec.VanillaBase["stamina"] = 5;
            var c = StatPlanner.Plan(rec, 2, R(PedAttribute.Health, 4), R(PedAttribute.Stamina, 5), 2, 3, false, out _).Single();
            Assert.Equal((4, 5), (c.From, c.To));
        }

        [Fact]
        public void TheBonusRankIsNeverTouched()
        {
            // Tack bonus +2 on health: only the base moves, and the change is judged on the base alone.
            var rec = Record(1, 0);
            rec.VanillaBase["health"] = 4;
            rec.VanillaBase["stamina"] = 5;
            var c = StatPlanner.Plan(rec, 2, R(PedAttribute.Health, 4, bonus: 2), R(PedAttribute.Stamina, 5), 2, 3, false, out _).Single();
            Assert.Equal(5, c.To);
        }

        [Theory]
        [InlineData(0, -2, 0)]
        [InlineData(9, 1, 9)]
        [InlineData(7, 1, 7)] // the game's max rank for this horse is 7
        public void RanksStayInsideTheGamesRange(int vanilla, int natural, int expected)
        {
            var rec = Record(natural, 0);
            rec.VanillaBase["health"] = vanilla;
            rec.VanillaBase["stamina"] = 3;
            int max = vanilla == 7 ? 7 : 9;
            var changes = StatPlanner.Plan(rec, 4, R(PedAttribute.Health, vanilla, max: max), R(PedAttribute.Stamina, 3), 2, 3, false, out _);
            Assert.DoesNotContain(changes, c => c.Attribute == PedAttribute.Health && c.To != expected);
        }

        [Fact]
        public void RestorePutsTheOriginalsBack()
        {
            var rec = Record(1, -2);
            rec.VanillaBase["health"] = 4;
            rec.VanillaBase["stamina"] = 5;
            var changes = StatPlanner.Plan(rec, 4, R(PedAttribute.Health, 5), R(PedAttribute.Stamina, 3), 2, 3, true, out var learned);
            Assert.Empty(learned);
            Assert.Equal(4, changes.Single(c => c.Attribute == PedAttribute.Health).To);
            Assert.Equal(5, changes.Single(c => c.Attribute == PedAttribute.Stamina).To);
        }

        [Fact]
        public void RestoreLeavesAStatTheModNeverStoredAlone()
        {
            var changes = StatPlanner.Plan(Record(1, 1), 4, R(PedAttribute.Health, 5), R(PedAttribute.Stamina, 3), 2, 3, true, out var learned);
            Assert.Empty(changes);
            Assert.Empty(learned);
        }

        [Fact]
        public void TemporaryPersonalitiesGetNoStatChanges()
        {
            var changes = StatPlanner.Plan(Record(1, 1, PersonalityStatus.Temporary), 4, R(PedAttribute.Health, 4), R(PedAttribute.Stamina, 5), 2, 3, false, out var learned);
            Assert.Empty(changes);
            Assert.Empty(learned);
        }

        [Fact]
        public void OriginalsAreSavedAndSurviveSaddling()
        {
            var catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
            ulong seed = 1;
            var reg = new HorseRegistry(new PersonalityRoller(catalog, ModSettings.Defaults()), new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 26));
            catalog.TryGetBreed("Morgan", out Breed morgan);
            var rec = reg.OnSaddle(5, new HorseFingerprint("a_c_horse_morgan_bay", true), morgan);
            reg.MarkSaved();
            reg.RememberVanillaBase(rec, "health", 4);
            Assert.True(reg.Dirty);
            reg.MarkSaved();
            reg.RememberVanillaBase(rec, "health", 4); // same value: nothing to save
            Assert.False(reg.Dirty);
            var back = HorseSaveFormat.Read(HorseSaveFormat.Write(reg.Permanent), new MemoryLog()).Single();
            Assert.Equal(4, back.VanillaBase["health"]);
        }
    }
}
