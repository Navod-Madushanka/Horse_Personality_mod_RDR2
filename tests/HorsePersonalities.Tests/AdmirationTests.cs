using System.Collections.Generic;
using HorsePersonalities.Core.Behavior;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Settings;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 8b: people compliment a clean, fit, bond-4 horse (Navod, Nov 28, 2026).</summary>
    public class AdmirationTests
    {
        private static readonly BehaviorTuning T = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog()).Behavior;

        private static AdmirationSituation Fine() => new AdmirationSituation
        {
            BondLevel = 4, HealthCore = 100, StaminaCore = 100, DirtRank = 0, Mounted = true, Speed = 1.5,
        };

        private static Admirer Person(int ped, double dist = 4, bool nice = true, bool good = false) =>
            new Admirer { Ped = ped, Distance = dist, HasNiceHorse = nice, HasGoodCondition = good };

        private static AdmirationBrain Brain(double roll = 0) => new AdmirationBrain(T, () => roll);

        [Fact]
        public void ShippedNumbersMatchTheDesign()
        {
            Assert.Equal(4, T.AdmireBond);
            Assert.Equal(70, T.AdmireMinCore);
            Assert.Equal(180, T.AdmireCooldown);
        }

        [Fact]
        public void ACleanFitBondFourHorseIsAdmired() => Assert.Null(Brain().WhyNotAdmired(Fine()));

        [Theory]
        [InlineData(3, 100, 100, 0, "bond")]
        [InlineData(4, 60, 100, 0, "cores")]
        [InlineData(4, 100, 50, 0, "cores")]
        [InlineData(4, 100, 100, 3, "dirt")]
        public void BondDirtOrHungerStopsIt(int bond, int health, int stamina, int dirt, string why)
        {
            var s = Fine();
            s.BondLevel = bond; s.HealthCore = health; s.StaminaCore = stamina; s.DirtRank = dirt;
            Assert.Contains(why, Brain().WhyNotAdmired(s));
            Assert.Equal(0, Brain().Pick(s, new[] { Person(7) }, 1000, 1).Ped);
        }

        [Fact]
        public void OnlyWhenRiddenSlowlyOrStoodBeside()
        {
            var b = Brain();
            var s = Fine();
            Assert.True(b.OnShow(s));
            s.Speed = 9;
            Assert.False(b.OnShow(s));                   // galloping past: nobody gets a look
            s = Fine(); s.Mounted = false; s.Speed = 0; s.PlayerDistance = 2;
            Assert.True(b.OnShow(s));
            s.PlayerDistance = 20;
            Assert.False(b.OnShow(s));                   // a horse left alone isn't admired
        }

        [Fact]
        public void TheNearestFriendlyPersonWithTheLineSpeaks()
        {
            var people = new List<Admirer>
            {
                Person(1, 2, nice: false),                           // no compliment in their voice
                new Admirer { Ped = 2, Distance = 1, HasNiceHorse = true, Unfriendly = true },
                new Admirer { Ped = 3, Distance = 1.5, HasNiceHorse = true, Speaking = true },
                Person(4, 20),                                       // out of range
                Person(5, 6),
                Person(6, 3),
            };
            var (ped, line) = Brain().Pick(Fine(), people, 1000, 1);
            Assert.Equal(6, ped);
            Assert.Equal(AdmirationBrain.NiceHorse, line);
        }

        [Fact]
        public void TheGangUsesTheirGoodConditionLine() =>
            Assert.Equal(AdmirationBrain.GoodCondition, Brain().Pick(Fine(), new[] { Person(9, 3, nice: true, good: true) }, 1000, 1).Line);

        [Fact]
        public void CooldownAndSamePersonLimit()
        {
            var b = Brain();
            var people = new[] { Person(1) };
            Assert.Equal(1, b.Pick(Fine(), people, 1000, 1).Ped);
            Assert.Equal(0, b.Pick(Fine(), people, 1000 + T.AdmireCooldown - 1, 1).Ped);   // cooldown
            Assert.Equal(0, b.Pick(Fine(), people, 1000 + T.AdmireCooldown + 1, 1).Ped);   // same person, too soon
            Assert.Equal(2, b.Pick(Fine(), new[] { Person(1), Person(2, 5) }, 1000 + T.AdmireCooldown + 2, 1).Ped);
            Assert.Equal(1, b.Pick(Fine(), people, 1000 + T.AdmireSamePersonMinutes * 60 + T.AdmireCooldown + 3, 1).Ped);
        }

        [Fact]
        public void TheRateRollDecidesWhenSomeoneIsInRange()
        {
            Assert.Equal(0, Brain(roll: 0.999).Pick(Fine(), new[] { Person(1) }, 1000, 1).Ped);
            Assert.Equal(1, Brain(roll: 0.0).Pick(Fine(), new[] { Person(1) }, 1000, 1).Ped);
        }

        [Fact]
        public void TheIniCanTurnItOff()
        {
            Assert.True(ModSettings.Defaults().NpcCompliments);
            Assert.False(ModSettings.Load("[HorsePersonalities]\nNpcCompliments = false\n", new MemoryLog()).NpcCompliments);
            Assert.Contains("NpcCompliments = true", ModSettings.DefaultIniText());
        }
    }
}
