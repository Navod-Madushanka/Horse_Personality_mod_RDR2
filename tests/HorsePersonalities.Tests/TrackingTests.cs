using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Tracking;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class TrackingTests
    {
        [Fact]
        public void ReportsMountDismountAndSwitchOnce()
        {
            var game = new FakeGameApi();
            game.AddHorse(10, 1);
            game.AddHorse(20, 2);
            var w = new MountWatcher();

            Assert.Equal(MountChangeKind.None, w.Update(game).Kind);
            game.Mount = 10;
            var c = w.Update(game);
            Assert.Equal(MountChangeKind.Mounted, c.Kind);
            Assert.Equal(10, c.Current);
            Assert.Equal(MountChangeKind.None, w.Update(game).Kind);

            game.Mount = 20;
            c = w.Update(game);
            Assert.Equal(MountChangeKind.Switched, c.Kind);
            Assert.Equal(10, c.Previous);

            game.Mount = 0;
            Assert.Equal(MountChangeKind.Dismounted, w.Update(game).Kind);
            Assert.Equal(0, w.CurrentMount);
        }

        [Fact]
        public void IgnoresAMountThatNoLongerExists()
        {
            var game = new FakeGameApi { Mount = 99 };
            Assert.Equal(MountChangeKind.None, new MountWatcher().Update(game).Kind);
        }

        [Fact]
        public void DescribesTheHorseForThePhase0Subtitle()
        {
            var catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horse_turkoman_gold"), male: false, bondRank: 2);
            var d = HorseDescriber.Describe(game, catalog, 10);
            Assert.Equal("Turkoman", d.Breed.Name);
            Assert.Equal("Turkoman - a_c_horse_turkoman_gold - female - bond 2", d.ToSubtitle());
        }

        [Fact]
        public void DescribesHorsesWithNoBreed()
        {
            var catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
            var game = new FakeGameApi();
            game.AddHorse(10, Joaat.Hash("a_c_horsemule_01"));
            game.AddHorse(11, 0xDEADBEEF);
            Assert.StartsWith("No breed (Mule) - a_c_horsemule_01", HorseDescriber.Describe(game, catalog, 10).ToSubtitle());
            Assert.StartsWith("Unknown breed - 0xDEADBEEF", HorseDescriber.Describe(game, catalog, 11).ToSubtitle());
        }
    }
}
