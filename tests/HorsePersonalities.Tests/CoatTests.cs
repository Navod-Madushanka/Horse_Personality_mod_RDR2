using System;
using System.Linq;
using HorsePersonalities.Core.Behavior;
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
    /// <summary>Build plan, Phase 7b: coat personalities (Navod's Arabian designs, Nov 27, 2026).</summary>
    public class CoatTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly BehaviorTuning T = Catalog.Behavior;
        private static readonly PersonalityRoller Roller = new PersonalityRoller(Catalog, ModSettings.Defaults());
        private static Breed Arabian { get { Catalog.TryGetBreed("Arabian", out Breed b); return b; } }

        private const string White = "a_c_horse_arabian_white", Black = "a_c_horse_arabian_black", RoseGreyBay = "a_c_horse_arabian_rosegreybay";

        // ---- Data ----

        [Fact]
        public void ShippedArabianCoatsLoad()
        {
            Assert.Equal(7, Catalog.CoatCount);
            Assert.True(Catalog.TryGetCoat(White, out Coat ice));
            Assert.Equal("The Ice Queen", ice.Archetype);
            Assert.Equal(new[] { Signature.TownDisdain, Signature.MudTantrum, Signature.SnakeDrama }, ice.Signatures);
            Assert.Contains("Oatcakes", ice.LovesTreats);
            Assert.Contains("Wild carrot", ice.IgnoresTreats);
            Assert.Equal(2, ice.Leans.Courage);
            Assert.False(Catalog.TryGetCoat("a_c_horse_morgan_bay", out _));
        }

        [Theory]
        [InlineData("\"model\": \"a_c_horse_arabian_black\"", "\"model\": \"a_c_horse_morgan_bay\"", "belongs to Morgan")]
        [InlineData("\"Dramatic rear\"", "\"DramaticRear\"", null)] // either spelling works
        [InlineData("\"DramaticRear\"", "\"Cliff jumper\"", "unknown signature")]
        [InlineData("\"lovesTreats\": [\"Oatcakes\"]", "\"lovesTreats\": [\"Caviar\"]", "not in treats.csv")]
        public void CoatRowsAreChecked(string find, string replace, string expected)
        {
            string json = TestData.BreedsJson;
            Assert.Contains(find, json.Contains(find) ? json : json.Replace("\"DramaticRear\"", find));
            json = json.Contains(find) ? json.Replace(find, replace) : json;
            Exception ex = Record.Exception(() => BreedCatalog.Load(json, new MemoryLog()));
            if (expected == null) Assert.Null(ex);
            else Assert.Contains(expected, Assert.IsType<BreedDataException>(ex).Message);
        }

        [Fact]
        public void SnakesAreKnownPredators()
        {
            uint snake = HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_snakeredboa_01");
            Assert.True(Catalog.IsPredator(snake));
            Assert.True(Catalog.IsSnake(snake));
            Assert.False(Catalog.IsSnake(HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_wolf")));
        }

        // ---- Rolls ----

        [Fact]
        public void CoatRollsCenterOnTheDesignAndKeepItsCharacter()
        {
            Catalog.TryGetCoat(White, out Coat ice);
            var rolls = Enumerable.Range(0, 2000).Select(i => Roller.Roll(Arabian, HorseOrigin.Wild, (ulong)i * 7919 + 1, false, White)).ToList();
            Assert.All(rolls, p => Assert.Equal("The Ice Queen", p.Archetype));
            Assert.All(rolls, p => Assert.Equal(ice.Signatures, p.Signatures));
            Assert.All(rolls, p => Assert.Contains(Quirk.HeadTosser, p.Quirks));
            Assert.InRange(rolls.Average(p => p.Traits.Courage), 1.4, 2.0); // clamped at +2, so the average sits a little under
            Assert.InRange(rolls.Average(p => p.Traits.BondAffinity), -1.4, -0.6);
            // Coat spread is tighter than breed spread: almost every Ice Queen is brave.
            Assert.True(rolls.Count(p => p.Traits.Courage > 0.5) > 1900);
        }

        [Fact]
        public void HorsesWithoutACoatRollAsBefore()
        {
            Catalog.TryGetBreed("Morgan", out Breed morgan);
            var a = Roller.Roll(morgan, HorseOrigin.Stable, 42, true, "a_c_horse_morgan_bay");
            var b = Roller.Roll(morgan, HorseOrigin.Stable, 42, true);
            Assert.Equal(b.ToString(), a.ToString());
            Assert.Null(a.Archetype);
            Assert.Empty(a.Signatures);
        }

        [Fact]
        public void SameSeedSameCoatHorse()
        {
            var a = Roller.Roll(Arabian, HorseOrigin.Stable, 99, true, Black);
            var b = Roller.Roll(Arabian, HorseOrigin.Stable, 99, true, Black);
            Assert.Equal(a.ToString(), b.ToString());
            Assert.Contains("Coat: The Dramatic Goth", string.Join("\n", a.Describe()));
        }

        [Fact]
        public void ExactDesignHasNoDice()
        {
            Catalog.TryGetCoat(RoseGreyBay, out Coat hero);
            for (ulong seed = 1; seed < 50; seed++)
            {
                var p = Roller.Roll(Arabian, HorseOrigin.Stable, seed, true, RoseGreyBay, exactDesign: true);
                Assert.Equal(hero.Leans.Nerves, p.Traits.Nerves, 6);
                Assert.Equal(new[] { Quirk.Shadow }, p.Quirks);
            }
        }

        [Fact]
        public void SignaturesSurviveTheSave()
        {
            ulong seed = 5;
            var reg = new HorseRegistry(Roller, new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 27));
            var id = new HorseFingerprint(Black, true);
            reg.OnMount(1, id, Arabian, HorseOrigin.Stable, true);
            var back = HorseSaveFormat.Read(HorseSaveFormat.Write(reg.Permanent), new MemoryLog()).Single();
            Assert.Equal("The Dramatic Goth", back.Personality.Archetype);
            Assert.Contains(Signature.EscapeArtist, back.Personality.Signatures);
        }

        [Fact]
        public void CoatDesignPresetGivesArabiansTheirDesignAndOthersAverage()
        {
            var presets = TestPresets.Load(TestData.Read("data", "test-personalities.json"), new MemoryLog());
            Assert.Equal("Average", presets.Presets[0].Name);
            TestPreset coat = presets.ForModel(White);
            Assert.Equal("Coat design", coat.Name);
            var ice = coat.For(Arabian, HorseOrigin.Stable, 3, White, Roller, Catalog);
            Assert.Equal(2, ice.Traits.Courage);
            Assert.Equal(-1, ice.Traits.BondAffinity);
            Assert.Contains(Signature.TownDisdain, ice.Signatures);
            Catalog.TryGetBreed("Morgan", out Breed morgan);
            var plain = coat.For(morgan, HorseOrigin.Stable, 3, "a_c_horse_morgan_bay", Roller, Catalog);
            Assert.Equal(0, plain.Traits.Courage);
            Assert.Empty(plain.Signatures);
        }

        // ---- Habits ----

        private static Personality With(params Signature[] sigs) =>
            new Personality("Arabian", HorseOrigin.Stable, 1, HorseQuality.Good, new Traits(0, 0, 0, 0), new[] { Quirk.Nosy },
                new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal, false, "Test coat", sigs);

        private static Decision S(Personality p, SignatureSituation s, double roll = 0, CooldownBook cd = null, SignatureBrain brain = null) =>
            (brain ?? new SignatureBrain(T, () => roll)).Decide(1, p, s, 0.25, cd ?? new CooldownBook(T, 120), 0);

        private static SignatureSituation Riding(double speed = 0, double still = 0) =>
            new SignatureSituation { Mounted = true, SpeedMs = speed, StandingStillSeconds = still, GameHour = 12, BondLevel = 2 };

        [Fact]
        public void NoSignaturesNoHabits()
        {
            var p = With();
            Assert.True(S(p, Riding(speed: 1.5)).IsNone);
        }

        [Fact]
        public void MoodyRefusalOnlyAtAWalk()
        {
            var p = With(Signature.MoodyRefusal);
            Assert.Equal(BehaviorAction.QuickStop, S(p, Riding(speed: 1.5)).Action);
            Assert.True(S(p, Riding(speed: T.GallopSpeed + 1)).IsNone);
            Assert.True(S(p, Riding(speed: 1.5), roll: 0.5).IsNone);
        }

        [Fact]
        public void DramaticRearWhenYouDrawAtDusk()
        {
            var p = With(Signature.DramaticRear);
            var s = Riding(still: 2); s.StartedAiming = true; s.GameHour = 19;
            Assert.Equal(BehaviorAction.Rear, S(p, s).Action);
            s.GameHour = 12;
            Assert.True(S(p, s).IsNone);
            s.GameHour = 19; s.StandingStillSeconds = 0;
            Assert.True(S(p, s).IsNone);
        }

        [Fact]
        public void EscapeArtistUnhitchesOncePerStop()
        {
            var p = With(Signature.EscapeArtist);
            var brain = new SignatureBrain(T, () => 0);
            var cd = new CooldownBook(T, 0);
            var s = new SignatureSituation { PlayerDistance = T.EscapePlayerDistance + 1, StandingStillSeconds = T.EscapeAfterSeconds };
            Assert.Equal(BehaviorAction.Unhitch, brain.Decide(1, p, s, 0.25, cd, 0).Action);
            Assert.True(brain.Decide(1, p, s, 0.25, cd, 10_000).IsNone); // still the same stop
            s.StandingStillSeconds = 0;
            brain.Decide(1, p, s, 0.25, cd, 10_001);
            s.StandingStillSeconds = T.EscapeAfterSeconds;
            Assert.Equal(BehaviorAction.Unhitch, brain.Decide(1, p, s, 0.25, cd, 20_000).Action);
            s.PlayerDistance = 2;
            s.StandingStillSeconds = 0; brain.Decide(1, p, s, 0.25, cd, 20_001); s.StandingStillSeconds = T.EscapeAfterSeconds;
            Assert.True(brain.Decide(1, p, s, 0.25, cd, 30_000).IsNone); // you're right there
        }

        [Fact]
        public void RoadsideGrazerGrazesWhenYouSitStill()
        {
            var p = With(Signature.RoadsideGrazer);
            Assert.Equal(BehaviorAction.Graze, S(p, Riding(still: T.GrazerIdleSeconds)).Action);
            Assert.True(S(p, Riding(still: T.GrazerIdleSeconds - 1)).IsNone);
        }

        [Fact]
        public void PranksterWaitsForTheQuietBeforeTheShooting()
        {
            var p = With(Signature.Prankster);
            var s = Riding(still: 1); s.EnemiesInCombat = 2;
            Assert.Equal(BehaviorAction.ThrowRider, S(p, s).Action);
            s.PlayerFiredRecently = true;
            Assert.True(S(p, s).IsNone);
        }

        [Fact]
        public void TownDisdainNeedsATownAndSomeoneClose()
        {
            var p = With(Signature.TownDisdain);
            var s = Riding(); s.InTown = true; s.TownPerson = 7;
            var d = S(p, s);
            Assert.Equal(BehaviorAction.Snub, d.Action);
            Assert.Equal(7, d.Target);
            s.InTown = false;
            Assert.True(S(p, s).IsNone);
        }

        [Fact]
        public void HillSighScatterbrainSoulStareCreeperMudTantrum()
        {
            var hill = Riding(speed: 1.5); hill.ClimbSpeed = T.HillClimbSpeed;
            Assert.Equal(BehaviorAction.Sigh, S(With(Signature.HillSigh), hill).Action);
            Assert.Equal(BehaviorAction.Skid, S(With(Signature.Scatterbrain), Riding(speed: T.GallopSpeed)).Action);
            var stranger = Riding(); stranger.StrangerClose = 9;
            Assert.Equal(BehaviorAction.Stare, S(With(Signature.SoulStare), stranger).Action);
            var camp = new SignatureSituation { PlayerNear = true, CreepTarget = 4 };
            Assert.Equal(BehaviorAction.Creep, S(With(Signature.CampCreeper), camp).Action);
            var muddy = Riding(); muddy.DirtRank = (int)T.FussyDirtRank;
            Assert.Equal(BehaviorAction.Agitate, S(With(Signature.MudTantrum), muddy).Action);
        }

        [Fact]
        public void SignaturesShareOneCooldown()
        {
            var p = With(Signature.MoodyRefusal);
            var cd = new CooldownBook(T, 120);
            var brain = new SignatureBrain(T, () => 0);
            Assert.False(brain.Decide(1, p, Riding(speed: 1.5), 0.25, cd, 0).IsNone);
            Assert.True(brain.Decide(1, p, Riding(speed: 1.5), 0.25, cd, T.SignatureCooldown - 1).IsNone);
            Assert.False(brain.Decide(1, p, Riding(speed: 1.5), 0.25, cd, T.SignatureCooldown + T.BehaviorGap).IsNone);
        }

        [Fact]
        public void LazyPaceCapsOnlyWithoutSpurs()
        {
            var brain = new SignatureBrain(T, () => 0);
            Assert.Equal(T.LazyWalkRatio, brain.LazyCap(With(Signature.LazyPace), true, false));
            Assert.Null(brain.LazyCap(With(Signature.LazyPace), true, true));
            Assert.Null(brain.LazyCap(With(), true, false));
        }

        [Fact]
        public void WolfBrawlerFightsInsteadOfFleeing()
        {
            var brain = new BehaviorBrain(T, ModSettings.Defaults(), () => 0);
            var s = new HorseSituation { PlayerNear = true, Threat = Threat.Predator, ThreatEntity = 33, GameHour = 12, BondLevel = 2 };
            Assert.Equal(BehaviorAction.Brawl, brain.Decide(1, With(Signature.WolfBrawler), s, 0.25, new CooldownBook(T, 120), 0).Action);
            Assert.NotEqual(BehaviorAction.Brawl, brain.Decide(1, With(), s, 0.25, new CooldownBook(T, 120), 0).Action);
        }

        [Fact]
        public void NightOwlAndCityLoverCalmRandomSpooks()
        {
            var brain = new BehaviorBrain(T, ModSettings.Defaults(), () => 0);
            var night = new HorseSituation { GameHour = 23, BondLevel = 2, Mounted = true };
            double owl = brain.RandomSpookRate(0, night, out _, With(Signature.NightOwl));
            double plain = brain.RandomSpookRate(0, night, out _, With());
            Assert.Equal(T.NightOwlMultiplier / T.NightSpookMultiplier, owl / plain, 6);
            var town = new HorseSituation { GameHour = 12, BondLevel = 2, Mounted = true, InTown = true };
            Assert.Equal(T.CityLoverMultiplier, brain.RandomSpookRate(0, town, out _, With(Signature.CityLover)) / brain.RandomSpookRate(0, town, out _, With()), 6);
        }

        [Fact]
        public void CoatTreatsAndBrushExcitement()
        {
            Catalog.TryGetCoat(White, out Coat ice);
            var care = new CareReactor(T, () => 0.99);
            var p = With();
            Assert.Equal(BehaviorAction.Call, care.React(p, ice.Demeanor, CareKind.Feed, 2, 0, ice, "Oatcakes").First);
            Assert.Equal(BehaviorAction.Snub, care.React(p, ice.Demeanor, CareKind.Feed, 2, 0, ice, "wild carrot").First);
            Assert.Equal(BehaviorAction.None, care.React(p, ice.Demeanor, CareKind.Feed, 2, 0, ice, "Apple").First); // roll 0.99: no reaction
            var excited = new CareReactor(T, () => 0);
            Assert.Equal(BehaviorAction.Agitate, excited.BrushStarted(With(Signature.BrushExcitement)).First);
            Assert.Equal(BehaviorAction.None, excited.BrushStarted(With()).First);
        }
    }
}
