using System;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Tracking;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class OverlayTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());

        private static HorseRegistry Registry()
        {
            ulong seed = 5;
            return new HorseRegistry(new PersonalityRoller(Catalog, ModSettings.Defaults()), new MemoryLog(), () => seed++, () => new DateTime(2026, 11, 24));
        }

        [Fact]
        public void ShowsTheRollAndWhetherItIsKept()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horse_turkoman_gold"), bondRank: 3);
            HorseDescription d = HorseDescriber.Describe(game, Catalog, 10);
            var id = new HorseFingerprint(d.ModelName, d.IsMale);
            var reg = Registry();

            var temp = OverlayText.Build(d, reg.OnMount(10, id, d.Breed, HorseOrigin.Unknown, false));
            Assert.Contains("Turkoman (Standard tier) - a_c_horse_turkoman_gold - male - bond 3", temp);
            Assert.Contains("Fingerprint a_c_horse_turkoman_gold|male|default - temporary (gone on reload; saddle it to keep it)", temp);
            Assert.Contains(temp, l => l.StartsWith("Courage "));
            Assert.Contains(temp, l => l.StartsWith("Quirks: "));

            var kept = OverlayText.Build(d, reg.OnSaddle(10, id, d.Breed));
            Assert.Contains("Fingerprint a_c_horse_turkoman_gold|male|default - permanent, first seen 2026-11-24", kept);
        }

        [Fact]
        public void SaysWhenAHorseStaysVanilla()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horsemule_01"));
            var lines = OverlayText.Build(HorseDescriber.Describe(game, Catalog, 10), null);
            Assert.Contains(lines, l => l.Contains("stay vanilla in v1"));
        }
    }
}
