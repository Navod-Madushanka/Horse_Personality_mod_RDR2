using System.Collections.Generic;
using HorsePersonalities.Core.Breeds;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class LearningRateTests
    {
        private static readonly Dictionary<string, LearningRate> Classes = new Dictionary<string, LearningRate>
        {
            ["Superior"] = new LearningRate(1.5, 0.8),
            ["Race"] = new LearningRate(1.3, 0.7),
            ["Riding"] = new LearningRate(1.0, 1.0),
            ["Work"] = new LearningRate(0.9, 1.1),
            ["War"] = new LearningRate(0.8, 1.5),
            ["Draft"] = new LearningRate(0.6, 1.2),
        };

        [Theory]
        [InlineData(new[] { "Race", "War" }, 1.17, 1.35)]   // Turkoman, from the plan
        [InlineData(new[] { "War", "Work" }, 0.81, 1.35)]   // Mustang, from the plan
        [InlineData(new[] { "Race", "Work" }, 1.17, 0.99)]  // Missouri Fox Trotter
        [InlineData(new[] { "Draft" }, 0.6, 1.2)]           // single class: no penalty
        [InlineData(new[] { "Superior" }, 1.5, 0.8)]
        public void FollowsTheSheetFormula(string[] classes, double physical, double combat)
        {
            var r = LearningRates.Compute(classes, Classes, 0.10);
            Assert.Equal(physical, r.Physical, 6);
            Assert.Equal(combat, r.Combat, 6);
        }

        [Fact]
        public void UnknownClassThrows() =>
            Assert.Throws<KeyNotFoundException>(() => LearningRates.Compute(new[] { "Pony" }, Classes, 0.1));
    }
}
