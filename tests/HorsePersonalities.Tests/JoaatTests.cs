using HorsePersonalities.Core.Hashing;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class JoaatTests
    {
        [Fact]
        public void MatchesKnownGameHash()
        {
            // GET_HASH_KEY("adder") == 0xB779A091 is the textbook Rockstar joaat test value.
            Assert.Equal(0xB779A091u, Joaat.Hash("adder"));
        }

        [Fact]
        public void IgnoresCaseLikeTheGame() =>
            Assert.Equal(Joaat.Hash("a_c_horse_arabian_white"), Joaat.Hash("A_C_Horse_Arabian_White"));

        [Fact]
        public void EmptyIsZero() => Assert.Equal(0u, Joaat.Hash(""));
    }
}

namespace HorsePersonalities.Tests
{
    public class GameEventTests
    {
        [Theory]
        // Hashes from femga rdr3_discoveries AI/EVENTS/events.lua
        [InlineData("EVENT_PED_ANIMAL_INTERACTION", 0xB5B9BAB4u)]
        [InlineData("EVENT_HORSE_BROKEN", 0x0D078005u)]
        [InlineData("EVENT_CALM_PED", 0x89AB08C3u)]
        [InlineData("EVENT_PLAYER_MOUNT_WILD_HORSE", 0x9BB8CEB6u)]
        [InlineData("EVENT_PLAYER_HORSE_AGITATED_BY_ANIMAL", 0x62AE6A25u)]
        [InlineData("EVENT_HORSE_STARTED_BREAKING", 0xE201BC38u)]
        public void EventHashesAreJoaatOfTheirNames(string name, uint expected)
        {
            Assert.Equal(expected, HorsePersonalities.Core.Hashing.Joaat.Hash(name));
            Assert.True(HorsePersonalities.Core.GameApi.GameEvents.TryGet(expected, out var t));
            Assert.Equal(name, t.Name);
        }

        [Fact]
        public void DescribesInteractions()
        {
            uint brush = HorsePersonalities.Core.Hashing.Joaat.Hash("Interaction_Brush");
            Assert.Equal("Interaction_Brush", HorsePersonalities.Core.GameApi.GameEvents.DescribeInteraction(brush));
            Assert.Equal("0x00000005", HorsePersonalities.Core.GameApi.GameEvents.DescribeInteraction(5));
        }
    }
}
