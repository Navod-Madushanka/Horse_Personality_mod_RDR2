using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Tracking;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class PreviewTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());

        private static PreviewRolls Previews()
        {
            ulong next = 100;
            return new PreviewRolls(new PersonalityRoller(Catalog, ModSettings.Defaults()), () => next++);
        }

        [Fact]
        public void RollsEachHorseOnceAndKeepsIt()
        {
            var p = Previews();
            Catalog.TryGetBreed("Arabian", out Breed arabian);
            var first = p.For(10, arabian, out bool rolled);
            Assert.True(rolled);
            var again = p.For(10, arabian, out rolled);
            Assert.False(rolled);
            Assert.Same(first, again);
            Assert.NotEqual(first.Seed, p.For(11, arabian, out _).Seed);
        }

        [Fact]
        public void ReusedHandleWithAnotherBreedRollsAgain()
        {
            var p = Previews();
            Catalog.TryGetBreed("Arabian", out Breed arabian);
            Catalog.TryGetBreed("Shire", out Breed shire);
            p.For(10, arabian, out _);
            Assert.Equal("Shire", p.For(10, shire, out bool rolled).Breed);
            Assert.True(rolled);
        }

        [Fact]
        public void HorsesWithNoBreedGetNoPersonality()
        {
            Assert.Null(Previews().For(10, null, out bool rolled));
            Assert.False(rolled);
        }

        [Fact]
        public void OverlayShowsTheRoll()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horse_turkoman_gold"), bondRank: 3);
            HorseDescription d = HorseDescriber.Describe(game, Catalog, 10);
            var lines = OverlayText.Build(d, Previews().For(10, d.Breed, out _));
            Assert.Contains("Turkoman (Standard tier) - a_c_horse_turkoman_gold - male - bond 3", lines);
            Assert.Contains(lines, l => l.StartsWith("Courage "));
            Assert.Contains(lines, l => l.StartsWith("Quirks: "));
            Assert.Contains(lines, l => l.StartsWith("Breaking if wild: "));
        }

        [Fact]
        public void OverlaySaysWhenAHorseStaysVanilla()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horsemule_01"));
            var lines = OverlayText.Build(HorseDescriber.Describe(game, Catalog, 10), null);
            Assert.Contains(lines, l => l.Contains("stay vanilla in v1"));
        }
    }
}
