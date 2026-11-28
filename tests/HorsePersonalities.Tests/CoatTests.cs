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
            Assert.Equal(65, Catalog.CoatCount);
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
            // ---- Missouri Fox Trotter coats ----

        [Fact]
        public void FoxTrotterCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_missourifoxtrotter_silverdapplepinto", out Coat celeb));
            Assert.Equal("The Elusive Celebrity", celeb.Archetype);
            Assert.Contains(Signature.Wanderlust, celeb.Signatures);
            Assert.True(Catalog.TryGetCoat("a_c_horse_missourifoxtrotter_blueroan", out Coat baseline));
            Assert.Equal(new[] { Signature.Stoic }, baseline.Signatures);
        }

        [Fact]
        public void SilverDappleIsAPrizedCommunityFavorite()
        {
            const string model = "a_c_horse_missourifoxtrotter_silverdapplepinto";
            Assert.True(Catalog.TryGetPrized(model, out _));
            Catalog.TryGetBreed("Missouri Fox Trotter", out Breed mft);
            for (ulong seed = 1; seed < 300; seed++)
            {
                var p = Roller.Roll(mft, HorseOrigin.Wild, seed, true, model);
                Assert.Equal(HorseQuality.Good, p.Quality);
                Assert.NotEqual(BreakingDifficulty.Unbreakable, p.Breaking);
                Assert.True(p.Traits.BondAffinity >= -1 - 1e-9); // never below its design: no flaw
                Assert.Contains(Signature.Wanderlust, p.Signatures);
            }
        }

        [Fact]
        public void StoicGivesNothingBackForAPat()
        {
            var care = new CareReactor(T, () => 0);
            var warm = new Personality("Missouri Fox Trotter", HorseOrigin.Stable, 1, HorseQuality.Good, new Traits(0, 0, 0, 2), new[] { Quirk.Shadow },
                new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal, false, "Test", new[] { Signature.Stoic });
            Assert.Equal(BehaviorAction.None, care.React(warm, DemeanorTraits.None, CareKind.Pat, 1, 0).First);
            Assert.NotEqual(BehaviorAction.None, care.React(warm, DemeanorTraits.None, CareKind.Brush, 1, 0).First);
        }

        [Fact]
        public void WanderlustRoamsWithYouFarAwayOncePerStop()
        {
            var p = With(Signature.Wanderlust);
            var brain = new SignatureBrain(T, () => 0);
            var cd = new CooldownBook(T, 0);
            var s = new SignatureSituation { PlayerDistance = 300, StandingStillSeconds = T.WanderlustDelaySeconds }; // far: not near, not mounted
            Assert.Equal(BehaviorAction.Roam, brain.Decide(1, p, s, 0.25, cd, 0).Action);
            Assert.True(brain.Decide(1, p, s, 0.25, cd, 10_000).IsNone);
            s.PlayerDistance = T.WanderlustPlayerDistance - 1; s.PlayerNear = true;
            Assert.True(new SignatureBrain(T, () => 0).Decide(1, p, s, 0.25, new CooldownBook(T, 0), 0).IsNone);
        }

        [Fact]
        public void FarAwayOnlyTheLeaveMeHabitsWork()
        {
            Assert.True(SignatureBrain.WatchedFar(With(Signature.EscapeArtist)));
            Assert.False(SignatureBrain.WatchedFar(With(Signature.SoulStare)));
            var s = new SignatureSituation { PlayerDistance = 100, StrangerClose = 5, StandingStillSeconds = 1 };
            Assert.True(S(With(Signature.SoulStare, Signature.EscapeArtist), s).IsNone);
        }

        [Fact]
        public void BoredBuckOncePerAimThrowsOnlyAtLowBond()
        {
            var brain = new SignatureBrain(T, () => 0);
            var cd = new CooldownBook(T, 0);
            var s = Riding(still: 3); s.AimingSeconds = T.BoredAimSeconds; s.BondLevel = 1;
            Assert.Equal(BehaviorAction.ThrowRider, brain.Decide(1, With(Signature.BoredBuck), s, 0.25, cd, 0).Action);
            Assert.True(brain.Decide(1, With(Signature.BoredBuck), s, 0.25, cd, 10_000).IsNone); // same aim
            s.BondLevel = 4;
            Assert.Equal(BehaviorAction.Agitate, S(With(Signature.BoredBuck), s).Action);
            s.AimingSeconds = T.BoredAimSeconds - 1;
            Assert.True(S(With(Signature.BoredBuck), s).IsNone);
        }

        [Fact]
        public void ClumsyTripsAtATrotAndSnackMagnetStopsAtAWalk()
        {
            Assert.Equal(BehaviorAction.Skid, S(With(Signature.Clumsy), Riding(speed: (T.WalkSpeed + T.GallopSpeed) / 2)).Action);
            Assert.True(S(With(Signature.Clumsy), Riding(speed: T.GallopSpeed + 1)).IsNone);
            Assert.Equal(BehaviorAction.SnackStop, S(With(Signature.SnackMagnet), Riding(speed: 1.5)).Action);
        }
            // ---- Mustang coats ----

        [Fact]
        public void MustangCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_mustang_tigerstripedbay", out Coat merc));
            Assert.Equal("The Scarred Mercenary", merc.Archetype);
            Assert.Contains(Signature.BountyStomper, merc.Signatures);
            Assert.True(Catalog.TryGetCoat("a_c_horse_mustang_buckskin", out Coat baseline));
            Assert.Equal(new[] { Signature.ShortKing, Signature.SelectiveHearing }, baseline.Signatures);
            Assert.True(Catalog.TryGetCoat("a_c_horse_mustang_wildbay", out Coat bay));
            Assert.Equal(new[] { Quirk.Shadow, Quirk.Chatterbox }, bay.SignatureQuirks);
        }

        [Fact]
        public void ShortKingAndBountyStomperSquareUpButNeverFight()
        {
            var horse = Riding(); horse.OtherHorse = 12;
            var d = S(With(Signature.ShortKing), horse);
            Assert.Equal(BehaviorAction.SquareUp, d.Action);
            Assert.Equal(12, d.Target);
            var bounty = Riding(); bounty.HogtiedPerson = 8;
            Assert.Equal(BehaviorAction.SquareUp, S(With(Signature.BountyStomper), bounty).Action);
            Assert.True(S(With(Signature.BountyStomper), Riding()).IsNone);
        }

        [Fact]
        public void GuardDogStompsAtStrangersEvenWithYouFarAway()
        {
            Assert.True(SignatureBrain.WatchedFar(With(Signature.GuardDog)));
            var away = new SignatureSituation { PlayerDistance = 200, GuardTarget = 3 };
            Assert.Equal(BehaviorAction.SquareUp, S(With(Signature.GuardDog), away).Action);
            var riding = Riding(); riding.GuardTarget = 3;
            Assert.True(S(With(Signature.GuardDog), riding).IsNone); // only while you're off it
        }

        [Fact]
        public void UnhurriedCapsTheSpurOnlyWithoutDanger()
        {
            var brain = new SignatureBrain(T, () => 0);
            var p = With(Signature.Unhurried);
            Assert.Equal(T.UnhurriedRatio, brain.UnhurriedCap(p, true, true, T.DangerMemorySeconds + 1));
            Assert.Null(brain.UnhurriedCap(p, true, true, 5)); // danger a moment ago: it runs
            Assert.Null(brain.UnhurriedCap(p, true, false, 999));
            Assert.Null(brain.UnhurriedCap(With(), true, true, 999));
        }

        [Fact]
        public void SaddleHaterSelectiveHearingAndJealousy()
        {
            var yes = new SignatureBrain(T, () => 0);
            Assert.Equal(BehaviorAction.Agitate, yes.OnMounted(1, With(Signature.SaddleHater), new CooldownBook(T, 0), 0).Action);
            Assert.True(yes.OnMounted(1, With(), new CooldownBook(T, 0), 0).IsNone);
            Assert.Equal(BehaviorAction.Ignore, yes.OnWhistle(1, With(Signature.SelectiveHearing), false, new CooldownBook(T, 0), 0).Action);
            Assert.True(yes.OnWhistle(1, With(Signature.SelectiveHearing), true, new CooldownBook(T, 0), 0).IsNone);
            Assert.True(new SignatureBrain(T, () => 0.99).OnWhistle(1, With(Signature.SelectiveHearing), false, new CooldownBook(T, 0), 0).IsNone);
            Assert.Equal(BehaviorAction.JealousCall, yes.OnRival(1, With(Signature.Jealous), 5, "patted", new CooldownBook(T, 0), 0).Action);
            Assert.True(yes.OnRival(1, With(Signature.Jealous), T.JealousRange + 1, "patted", new CooldownBook(T, 0), 0).IsNone);
        }
            // ---- Turkoman coats ----

        [Fact]
        public void TurkomanCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_turkoman_darkbay", out Coat assassin));
            Assert.Equal("The Aristocratic Assassin", assassin.Archetype);
            Assert.Contains("Sugar cube", assassin.LovesTreats);
            Assert.Contains("Haycube", assassin.IgnoresTreats);
            Assert.True(Catalog.TryGetCoat("a_c_horse_turkoman_perlino", out Coat baseline));
            Assert.Equal(new[] { Signature.Restless, Signature.TrailSnob }, baseline.Signatures);
        }

        [Fact]
        public void RestlessAndTrailSnob()
        {
            Assert.Equal(BehaviorAction.Agitate, S(With(Signature.Restless), Riding(still: T.RestlessSeconds)).Action);
            Assert.True(S(With(Signature.Restless), Riding(still: T.RestlessSeconds - 1)).IsNone);
            var walk = Riding(speed: 1.5); walk.WalkingSeconds = T.TrailSnobSeconds;
            Assert.Equal(BehaviorAction.Snub, S(With(Signature.TrailSnob), walk).Action);
            walk.WalkingSeconds = 5;
            Assert.True(S(With(Signature.TrailSnob), walk).IsNone);
        }

        [Fact]
        public void WarHorseNeverSpooksInAFightAndFightsWhenYoureOff()
        {
            var brain = new BehaviorBrain(T, ModSettings.Defaults(), () => 0);
            var fight = new HorseSituation { Mounted = true, Threat = Threat.Gunfire, InFight = true, GameHour = 12, BondLevel = 2 };
            Assert.True(brain.Decide(1, With(Signature.WarHorse), fight, 0.25, new CooldownBook(T, 120), 0).IsNone);
            Assert.False(brain.Decide(1, With(), fight, 0.25, new CooldownBook(T, 120), 0).IsNone);
            var off = new SignatureSituation { PlayerNear = true, NearEnemy = 21 };
            var d = S(With(Signature.WarHorse), off);
            Assert.Equal(BehaviorAction.Brawl, d.Action);
            Assert.Equal(21, d.Target);
        }

        [Fact]
        public void SnobShowboatMustLeadVain()
        {
            var cheap = Riding(); cheap.LowerTierHorse = 5;
            Assert.Equal(BehaviorAction.Snub, S(With(Signature.Snob), cheap).Action);
            var stop = Riding(still: 1); stop.JustStoppedFromGallop = true; stop.Crowd = (int)T.ShowboatCrowd;
            Assert.Equal(BehaviorAction.Rear, S(With(Signature.Showboat), stop).Action);
            stop.Crowd = 0;
            Assert.True(S(With(Signature.Showboat), stop).IsNone);
            var behind = Riding(speed: T.WalkSpeed + 1); behind.HorseAhead = 4;
            Assert.Equal(BehaviorAction.Agitate, S(With(Signature.MustLead), behind).Action);
            var care = new CareReactor(T, () => 0.99);
            Assert.Equal(BehaviorAction.Content, care.React(With(Signature.Vain), DemeanorTraits.None, CareKind.Brush, 4, 0).First);
        }
            // ---- American Standardbred coats ----

        [Fact]
        public void StandardbredCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_americanstandardbred_palominodapple", out Coat pet));
            Assert.Equal("The Teacher's Pet", pet.Archetype);
            Assert.Equal(-2, pet.Leans.Courage);
            Assert.True(Catalog.TryGetCoat("a_c_horse_americanstandardbred_lightbuckskin", out Coat baseline));
            Assert.Equal(new[] { Signature.SnakeDrama, Signature.HillSigh }, baseline.Signatures);
        }

        [Fact]
        public void RacerCallsAChallenger()
        {
            var s = Riding(speed: T.WalkSpeed + 2); s.Challenger = 6;
            Assert.Equal(BehaviorAction.Call, S(With(Signature.Racer), s).Action);
            s.Challenger = 0;
            Assert.True(S(With(Signature.Racer), s).IsNone);
        }

        [Fact]
        public void NoBrakesSkidsOnAHardStopOnce()
        {
            var brain = new SignatureBrain(T, () => 0);
            var cd = new CooldownBook(T, 0);
            var stop = Riding(speed: 1); stop.SecondsSinceGallop = 1;
            Assert.Equal(BehaviorAction.Skid, brain.Decide(1, With(Signature.NoBrakes), stop, 0.25, cd, 0).Action);
            Assert.True(brain.Decide(1, With(Signature.NoBrakes), stop, 0.25, cd, 10_000).IsNone); // same stop
            var slow = Riding(speed: 1); slow.SecondsSinceGallop = T.NoBrakesSeconds + 2;
            Assert.True(S(With(Signature.NoBrakes), slow).IsNone); // eased down, not a hard stop
        }

        [Fact]
        public void SpoiledWantsATreatAfterALongGallop()
        {
            var stop = Riding(speed: 0, still: 1); stop.SecondsSinceGallop = 3; stop.LastGallopSeconds = T.SpoiledGallopSeconds;
            Assert.Equal(BehaviorAction.Call, S(With(Signature.Spoiled), stop).Action);
            stop.LastGallopSeconds = 5;
            Assert.True(S(With(Signature.Spoiled), stop).IsNone);
        }

        [Fact]
        public void SeparationAnxietyWhinniesWhenYouWalkAwayEvenFar()
        {
            Assert.True(SignatureBrain.WatchedFar(With(Signature.SeparationAnxiety)));
            var away = new SignatureSituation { PlayerNear = true, PlayerDistance = T.SeparationDistance, StandingStillSeconds = 2 };
            Assert.Equal(BehaviorAction.Call, S(With(Signature.SeparationAnxiety), away).Action);
            away.PlayerNear = false; away.PlayerDistance = 300;
            Assert.Equal(BehaviorAction.Call, S(With(Signature.SeparationAnxiety), away).Action);
            away.PlayerDistance = 1; away.PlayerNear = true;
            Assert.True(S(With(Signature.SeparationAnxiety), away).IsNone);
        }
            // ---- Nokota coats ----

        [Fact]
        public void NokotaCoatsLoadAndAllHoldGrudges()
        {
            foreach (string m in new[] { "reversedappleroan", "blueroan", "whiteroan" })
            {
                Assert.True(Catalog.TryGetCoat("a_c_horse_nokota_" + m, out Coat c));
                Assert.Contains(Signature.Grudge, c.Signatures);
            }
            Catalog.TryGetCoat("a_c_horse_nokota_blueroan", out Coat survivor);
            Assert.Contains("Oatcakes", survivor.LovesTreats);
        }

        [Fact]
        public void GrudgeSulksAfterAHurtAndOnlyForGrudgeHorses()
        {
            var brain = new SignatureBrain(T, () => 0);
            Assert.Equal(BehaviorAction.Agitate, brain.OnHurt(1, With(Signature.Grudge), new CooldownBook(T, 0), 0).Action);
            Assert.True(brain.OnHurt(1, With(), new CooldownBook(T, 0), 0).IsNone);
            var care = new CareReactor(T, () => 0);
            var warm = new Personality("Nokota", HorseOrigin.Stable, 1, HorseQuality.Good, new Traits(0, 0, 0, 2), new[] { Quirk.Nosy },
                new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal, false, "Test", new[] { Signature.Grudge });
            Assert.Equal(BehaviorAction.None, care.React(warm, DemeanorTraits.None, CareKind.Pat, 1, 0, sulking: true).First);
            Assert.NotEqual(BehaviorAction.None, care.React(warm, DemeanorTraits.None, CareKind.Pat, 1, 0, sulking: false).First);
        }

        [Fact]
        public void DownhillSnobAndStrangerDanger()
        {
            var down = Riding(speed: 1.5); down.ClimbSpeed = -T.DownhillSpeed;
            Assert.Equal(BehaviorAction.Snub, S(With(Signature.DownhillSnob), down).Action);
            down.ClimbSpeed = T.DownhillSpeed; // uphill
            Assert.True(S(With(Signature.DownhillSnob), down).IsNone);
            var rush = Riding(); rush.RushingPerson = 14;
            var d = S(With(Signature.StrangerDanger), rush);
            Assert.Equal(BehaviorAction.SquareUp, d.Action);
            Assert.Equal(14, d.Target);
        }
            // ---- Thoroughbred coats ----

        [Fact]
        public void ThoroughbredCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_thoroughbred_dapplegrey", out Coat vet));
            Assert.Equal("The Loyal Veteran", vet.Archetype);
            Assert.Contains("Apple", vet.LovesTreats);
            Assert.True(Catalog.TryGetCoat("a_c_horse_thoroughbred_blackchestnut", out Coat baseline));
            Assert.Equal(new[] { Signature.NeedsSoothing, Signature.Clumsy }, baseline.Signatures);
            Assert.False(Catalog.TryGetCoat("a_c_horse_gang_john", out _)); // Rachel stays vanilla
        }

        [Fact]
        public void NeedsSoothingFretsAfterDangerUntilPatted()
        {
            var p = With(Signature.NeedsSoothing);
            var s = Riding(); s.SecondsSinceDanger = 30;
            Assert.Equal(BehaviorAction.Fret, S(p, s).Action);
            s.SoothedSinceDanger = true;
            Assert.True(S(p, s).IsNone);
            s.SoothedSinceDanger = false; s.SecondsSinceDanger = T.NeedsSoothingSeconds + 1;
            Assert.True(S(p, s).IsNone); // it got over it
            s.SecondsSinceDanger = 1;
            Assert.True(S(p, s).IsNone); // not yet
        }

        [Fact]
        public void HillCrawlerWaterSighAppleBeggar()
        {
            var brain = new SignatureBrain(T, () => 0);
            Assert.Equal(T.HillCrawlRatio, brain.HillCap(With(Signature.HillCrawler), true, T.HillClimbSpeed));
            Assert.Null(brain.HillCap(With(Signature.HillCrawler), true, 0));
            var wet = Riding(speed: 2); wet.InWater = true;
            Assert.Equal(BehaviorAction.Sigh, S(With(Signature.WaterSigh), wet).Action);
            var beg = new SignatureSituation { PlayerNear = true, PlayerDistance = 1.5, StandingStillSeconds = T.BeggarSeconds };
            Assert.Equal(BehaviorAction.Beg, S(With(Signature.AppleBeggar), beg).Action);
            beg.PlayerDistance = 8;
            Assert.True(S(With(Signature.AppleBeggar), beg).IsNone);
        }
            // ---- Andalusian coats ----

        [Fact]
        public void AndalusianCoatsAreAllWarHorses()
        {
            foreach (string m in new[] { "perlino", "darkbay", "rosegray" })
            {
                Assert.True(Catalog.TryGetCoat("a_c_horse_andalusian_" + m, out Coat c));
                Assert.Contains(Signature.WarHorse, c.Signatures);
            }
        }

        [Fact]
        public void HatesBrushingAlwaysGrumbles()
        {
            var care = new CareReactor(T, () => 0.99); // a roll that would normally give no reaction
            var r = care.React(With(Signature.HatesBrushing), DemeanorTraits.None, CareKind.Brush, 4, 0);
            Assert.Equal(BehaviorAction.Agitate, r.First);
            Assert.Equal(BehaviorAction.Snort, r.Second);
            Assert.Equal(BehaviorAction.None, care.React(With(Signature.HatesBrushing), DemeanorTraits.None, CareKind.Pat, 4, 0).First);
        }
            // ---- Ardennes coats ----

        [Fact]
        public void ArdennesCoatsLoad()
        {
            foreach (string m in new[] { "strawberryroan", "bayroan", "irongreyroan" })
            {
                Assert.True(Catalog.TryGetCoat("a_c_horse_ardennes_" + m, out Coat c));
                Assert.Contains(Signature.WarHorse, c.Signatures);
            }
            Catalog.TryGetCoat("a_c_horse_ardennes_strawberryroan", out Coat brawler);
            Assert.DoesNotContain(Signature.Unhurried, brawler.Signatures); // it begs to charge
        }

        [Fact]
        public void BalkerPlantsItsHoovesOnSteepClimbsAndInWater()
        {
            var climb = Riding(speed: 1.5); climb.ClimbSpeed = T.HillClimbSpeed;
            Assert.Equal(BehaviorAction.QuickStop, S(With(Signature.Balker), climb).Action);
            var river = Riding(speed: 2); river.InWater = true;
            Assert.Equal(BehaviorAction.QuickStop, S(With(Signature.Balker), river).Action);
            Assert.True(S(With(Signature.Balker), Riding(speed: 2)).IsNone);
        }
            // ---- Hungarian Halfbred coats ----

        [Fact]
        public void HungarianHalfbredCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_hungarianhalfbred_liverchestnut", out Coat baseline));
            Assert.Equal(new[] { Signature.WarHorse, Signature.TrailSnob }, baseline.Signatures);
            Assert.True(Catalog.TryGetCoat("a_c_horse_hungarianhalfbred_darkdapplegrey", out Coat officer));
            Assert.Contains(Signature.Strict, officer.Signatures);
        }

        [Fact]
        public void StrictSnortsAndRefusesToSprintAfterSloppyRiding()
        {
            var brain = new SignatureBrain(T, () => 0);
            Assert.Equal(BehaviorAction.Snub, brain.OnHurt(1, With(Signature.Strict), new CooldownBook(T, 0), 0).Action);
            Assert.Equal(T.StrictRatio, brain.StrictCap(With(Signature.Strict), true, true, true));
            Assert.Null(brain.StrictCap(With(Signature.Strict), true, true, false)); // the 30 s are over
            Assert.Null(brain.StrictCap(With(Signature.Strict), true, false, true)); // not spurring
            Assert.Null(brain.StrictCap(With(), true, true, true));
        }
            [Fact]
        public void DogsAreKnownKickerTargetsButNotPredators()
        {
            Assert.Equal(12, Catalog.DogModelCount);
            Assert.False(Catalog.IsDog(HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_dogrufus_01"))); // Rufus stays safe
            Assert.False(Catalog.IsDog(HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_doghobo_01")));  // Cain (to confirm)
            uint lab = HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_doglab_01");
            Assert.True(Catalog.IsDog(lab));
            Assert.False(Catalog.IsPredator(lab));
            Assert.False(Catalog.IsDog(HorsePersonalities.Core.Hashing.Joaat.Hash("a_c_wolf")));
        }
            // ---- American Paint coats ----

        [Fact]
        public void PaintCoatsAllWanderOffForCarrots()
        {
            foreach (string m in new[] { "splashedwhite", "overo", "tobiano", "greyovero" })
            {
                Assert.True(Catalog.TryGetCoat("a_c_horse_americanpaint_" + m, out Coat c));
                Assert.Contains(Signature.SnackMagnet, c.Signatures);
                Assert.Contains("Wild carrot", c.LovesTreats);
            }
            Catalog.TryGetCoat("a_c_horse_americanpaint_greyovero", out Coat scout);
            Assert.Contains("Sugar cube", scout.IgnoresTreats);
        }

        [Fact]
        public void SixthSenseNightStareCuriousBumpkin()
        {
            var sensed = Riding(); sensed.SensedPredator = 40;
            var d = S(With(Signature.SixthSense), sensed);
            Assert.Equal(BehaviorAction.Snub, d.Action);
            Assert.Equal(40, d.Target);
            var night = new SignatureSituation { PlayerNear = true, StandingStillSeconds = 3, GameHour = 23 };
            Assert.Equal(BehaviorAction.StareAtNothing, S(With(Signature.NightStare), night).Action);
            night.GameHour = 12;
            Assert.True(S(With(Signature.NightStare), night).IsNone);
            var off = new SignatureSituation { PlayerNear = true, CuriousTarget = 9 };
            Assert.Equal(BehaviorAction.Creep, S(With(Signature.Curious), off).Action);
            var town = Riding(speed: 2); town.InTown = true;
            Assert.Equal(BehaviorAction.QuickStop, S(With(Signature.CountryBumpkin), town).Action);
        }

        [Fact]
        public void DeadpanStaresAfterAHurt()
        {
            var brain = new SignatureBrain(T, () => 0);
            Assert.Equal(BehaviorAction.Stare, brain.OnHurt(1, With(Signature.Deadpan), new CooldownBook(T, 0), 0).Action);
        }
            // ---- Appaloosa coats ----

        [Fact]
        public void AppaloosaCoatsLoad()
        {
            Assert.True(Catalog.TryGetCoat("a_c_horse_appaloosa_blacksnowflake", out Coat baseline));
            Assert.Equal(new[] { Signature.TrailSnob }, baseline.Signatures);
            Assert.True(Catalog.TryGetCoat("a_c_horse_appaloosa_blanket", out Coat a));
            Assert.True(Catalog.TryGetCoat("a_c_horse_appaloosa_leopardblanket", out Coat b));
            Assert.Equal(a.Archetype, b.Archetype);
            Assert.True(Catalog.TryGetPrized("a_c_horse_appaloosa_fewspotted_pc", out _));
        }

        [Fact]
        public void CrowdShySideStepsAndWontEatInFrontOfStrangers()
        {
            var off = new SignatureSituation { PlayerNear = true, CrowdPerson = 3 };
            Assert.Equal(BehaviorAction.SideStep, S(With(Signature.CrowdShy), off).Action);
            var care = new CareReactor(T, () => 0);
            Assert.Equal(BehaviorAction.Snort, care.React(With(Signature.CrowdShy), DemeanorTraits.None, CareKind.Feed, 2, 0, strangerNear: true).First);
        }

        [Fact]
        public void BribableClimbsForAnOatcake()
        {
            var brain = new SignatureBrain(T, () => 0);
            var p = With(Signature.HillCrawler, Signature.Bribable);
            Assert.Equal(T.HillCrawlRatio, brain.HillCap(p, true, T.HillClimbSpeed));
            Assert.Null(brain.HillCap(p, true, T.HillClimbSpeed, bribed: true));
            Assert.Equal(T.HillCrawlRatio, brain.HillCap(With(Signature.HillCrawler), true, T.HillClimbSpeed, bribed: true)); // not bribable
        }
            // ---- Dutch Warmblood coats ----

        [Fact]
        public void WarmbloodCoatsAllHateSpurSpam()
        {
            foreach (string m in new[] { "chocolateroan", "sootybuckskin", "sealbrown" })
            {
                Assert.True(Catalog.TryGetCoat("a_c_horse_dutchwarmblood_" + m, out Coat c));
                Assert.Contains(Signature.SpurSnob, c.Signatures);
            }
            Assert.False(Catalog.TryGetCoat("a_c_horse_buell_warvets", out _)); // Buell stays vanilla (v1 rule) until decided
        }

        [Fact]
        public void SpurSnobSnortsAtSpurSpam()
        {
            var s = Riding(speed: 5); s.RecentSpurPresses = (int)T.SpurSpamPresses;
            Assert.Equal(BehaviorAction.Snub, S(With(Signature.SpurSnob), s).Action);
            s.RecentSpurPresses = (int)T.SpurSpamPresses - 1;
            Assert.True(S(With(Signature.SpurSnob), s).IsNone);
        }
    }
}
