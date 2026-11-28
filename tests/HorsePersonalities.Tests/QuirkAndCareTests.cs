using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Behavior;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using Xunit;

namespace HorsePersonalities.Tests
{
    /// <summary>Build plan, Phase 7 (decisions of Nov 26, 2026).</summary>
    public class QuirkAndCareTests
    {
        private static readonly BreedCatalog Catalog = BreedCatalog.Load(TestData.BreedsJson, new MemoryLog());
        private static readonly BehaviorTuning T = Catalog.Behavior;

        private static Personality Horse(params Quirk[] quirks) => Horse(0, 0, quirks);

        private static Personality Horse(double temper, double bond, params Quirk[] quirks) =>
            new Personality("Morgan", HorseOrigin.Stable, 0xABCDEF, HorseQuality.Good, new Traits(0, 0, temper, bond), quirks,
                new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal);

        private static QuirkSituation Near(double still = 0, double distance = 2) =>
            new QuirkSituation { PlayerNear = true, PlayerDistance = distance, StandingStillSeconds = still };

        private static Decision Q(Personality p, QuirkSituation s, double roll = 0, DemeanorTraits demeanor = null, CooldownBook cd = null) =>
            new QuirkBrain(T, () => roll).Decide(1, p, demeanor ?? DemeanorTraits.None, s, 0.25, cd ?? new CooldownBook(T, 120), 0);

        // ---- Quirks ----

        [Fact]
        public void ShadowFollowsWhenYouWalkAway()
        {
            var s = Near(); s.PlayerWalkedAway = true;
            Assert.Equal(BehaviorAction.Follow, Q(Horse(Quirk.Shadow), s).Action);
            Assert.True(Q(Horse(Quirk.Nosy), s).IsNone);
        }

        [Fact]
        public void WandererWandersWhenLeftStandingWithYouAway()
        {
            Assert.Equal(BehaviorAction.Wander, Q(Horse(Quirk.Wanderer), Near(still: T.WandererDelaySeconds, distance: T.WandererPlayerDistance)).Action);
            Assert.True(Q(Horse(Quirk.Wanderer), Near(still: T.WandererDelaySeconds - 1, distance: 10)).IsNone);
            Assert.True(Q(Horse(Quirk.Wanderer), Near(still: 60, distance: 2)).IsNone); // you're right next to it
        }

        [Fact]
        public void KickerKicksSomeoneBehindItEvenWithWitnesses()
        {
            var s = Near(still: 1); s.NpcBehind = 42;
            var d = Q(Horse(Quirk.Kicker), s);
            Assert.Equal(BehaviorAction.KickNpc, d.Action);
            Assert.Equal(42, d.Target);
            // Witnesses don't stop it any more (Navod, Nov 28): a kick in town may bring the law.
            Assert.True(Q(Horse(Quirk.Kicker), s, roll: T.KickerChance + 0.01).IsNone);
            var moving = Near(still: 0); moving.NpcBehind = 42;
            Assert.True(Q(Horse(Quirk.Kicker), moving).IsNone); // only while standing still
        }

        [Fact]
        public void HeadTosserTossesWhileStandingStill()
        {
            Assert.Equal(BehaviorAction.Agitate, Q(Horse(Quirk.HeadTosser), Near(still: 1)).Action);
            Assert.True(Q(Horse(Quirk.HeadTosser), Near(still: 0)).IsNone);
        }

        [Fact]
        public void NosyWatchesSomeonePassing()
        {
            var s = Near(); s.PasserBy = 7;
            var d = Q(Horse(Quirk.Nosy), s);
            Assert.Equal(BehaviorAction.LookAt, d.Action);
            Assert.Equal(7, d.Target);
            Assert.True(Q(Horse(Quirk.Nosy), Near()).IsNone);
        }

        [Fact]
        public void ChatterboxCallsOutMoreWithHorsesAround()
        {
            Assert.Contains(Q(Horse(Quirk.Chatterbox), Near()).Action, new[] { BehaviorAction.Call, BehaviorAction.Snort });
            var rng = new Random(3);
            int Count(bool horseNear)
            {
                var brain = new QuirkBrain(T, () => rng.NextDouble());
                var s = Near(); s.OtherHorseNear = horseNear;
                int n = 0;
                for (int i = 0; i < 40_000; i++) if (!brain.Decide(1, Horse(Quirk.Chatterbox), DemeanorTraits.None, s, 0.25, new CooldownBook(T, 0), 0).IsNone) n++;
                return n;
            }
            int alone = Count(false), crowd = Count(true);
            Assert.InRange(crowd / (double)alone, 1.6, 2.4);
            Assert.InRange(alone, 80, 120); // 0.6/min over 40,000 x 0.25 s = 166 min: about 100
        }

        [Fact]
        public void QuirksWaitForTheQuirkCooldown()
        {
            var cd = new CooldownBook(T, 120);
            var s = Near(still: 1);
            Assert.False(Q(Horse(Quirk.HeadTosser), s, cd: cd).IsNone);
            Assert.False(cd.Ready(1, BehaviorGroup.Quirk, 119));
            Assert.True(cd.Ready(1, BehaviorGroup.Quirk, 120));
        }

        [Fact]
        public void NoQuirkForAHorseYouAreNeitherRidingNorNear()
        {
            var s = new QuirkSituation { StandingStillSeconds = 100, PasserBy = 1, PlayerWalkedAway = true };
            Assert.True(Q(Horse(Quirk.HeadTosser, Quirk.Nosy), s).IsNone);
        }

        // ---- Demeanor ----

        [Fact]
        public void ImpatientBreedsFidgetWhenKeptStanding()
        {
            var turkoman = new DemeanorTraits(2, 0, 0, 0);
            Assert.Equal(BehaviorAction.Agitate, Q(Horse(), Near(still: T.ImpatientAfterSeconds), demeanor: turkoman).Action);
            Assert.True(Q(Horse(), Near(still: T.ImpatientAfterSeconds - 1), demeanor: turkoman).IsNone);
            Assert.True(Q(Horse(), Near(still: 600), demeanor: DemeanorTraits.None).IsNone);
        }

        [Fact]
        public void AlertBreedsWatchAndFussyOnesComplainWhenDirty()
        {
            var s = Near(); s.PasserBy = 5;
            Assert.Equal(BehaviorAction.LookAt, Q(Horse(), s, demeanor: new DemeanorTraits(0, 2, 0, 0)).Action);
            var dirty = Near(); dirty.DirtRank = (int)T.FussyDirtRank;
            Assert.Equal(BehaviorAction.Snort, Q(Horse(), dirty, demeanor: new DemeanorTraits(0, 0, 0, 2)).Action);
            Assert.Equal(BehaviorGroup.Tell, Q(Horse(), dirty, demeanor: new DemeanorTraits(0, 0, 0, 2)).Group);
        }

        [Fact]
        public void EveryShippedBreedHasDemeanorTraits()
        {
            Assert.True(Catalog.TryGetBreed("Turkoman", out Breed t));
            Assert.Equal(2, t.DemeanorTraits.Impatient);
            Assert.True(Catalog.TryGetBreed("Arabian", out Breed a));
            Assert.Equal(2, a.DemeanorTraits.Alert);
            Assert.True(Catalog.TryGetBreed("Belgian Draft", out Breed b));
            Assert.Equal(2, b.DemeanorTraits.Affectionate);
        }

        // ---- Care reactions ----

        private static CareReactor Care(double roll = 0) => new CareReactor(T, () => roll);

        [Theory]
        [InlineData(0, 1.5, ReactionStyle.Affectionate)]
        [InlineData(1.0, 1.5, ReactionStyle.Grumpy)]   // Temper wins
        [InlineData(0, -1.0, ReactionStyle.Aloof)]
        [InlineData(0, 0.3, ReactionStyle.Neutral)]
        public void StyleComesFromTemperAndBondAffinity(double temper, double bond, ReactionStyle expected) =>
            Assert.Equal(expected, Care().Style(Horse(temper, bond), DemeanorTraits.None));

        [Fact]
        public void AnAffectionateBreedLeansAffectionate()
        {
            Assert.Equal(ReactionStyle.Neutral, Care().Style(Horse(0, 0.3), DemeanorTraits.None));
            Assert.Equal(ReactionStyle.Affectionate, Care().Style(Horse(0, 0.3), new DemeanorTraits(0, 0, 2, 0))); // drafts lean into pats
        }

        [Theory]
        [InlineData(0, 1.5, CareKind.Pat, BehaviorAction.Content)]
        [InlineData(0, 1.5, CareKind.Feed, BehaviorAction.Content)]
        [InlineData(0, 1.5, CareKind.Calm, BehaviorAction.None)]
        [InlineData(1.5, 0, CareKind.Pat, BehaviorAction.Agitate)]
        [InlineData(1.5, 0, CareKind.Brush, BehaviorAction.Agitate)]
        [InlineData(1.5, 0, CareKind.Calm, BehaviorAction.Agitate)]
        [InlineData(0, -1.5, CareKind.Pat, BehaviorAction.None)]
        [InlineData(0, -1.5, CareKind.Brush, BehaviorAction.Agitate)]
        [InlineData(0, 0, CareKind.Pat, BehaviorAction.None)]
        public void ReactionsMatchThePlansTable(double temper, double bond, CareKind kind, BehaviorAction expected) =>
            Assert.Equal(expected, Care().React(Horse(temper, bond), DemeanorTraits.None, kind, 1, 0).First);

        [Fact]
        public void ReactionsSoftenAsTheBondGrows()
        {
            // Chance 0.9 at bond 1, 0.9 x (1 - 0.2 x 3) = 0.36 at bond 4: roll 0.5 reacts at bond 1, not at bond 4.
            Assert.Equal(BehaviorAction.Agitate, Care(0.5).React(Horse(1.5, 0), DemeanorTraits.None, CareKind.Pat, 1, 0).First);
            Assert.Equal(BehaviorAction.None, Care(0.5).React(Horse(1.5, 0), DemeanorTraits.None, CareKind.Pat, 4, 0).First);
        }

        [Fact]
        public void APickyEaterAlwaysAnswersTheSameTreatTheSameWay()
        {
            var picky = Horse(Quirk.PickyEater);
            var reactor = Care();
            var tastes = Enumerable.Range(1, 300).Select(i => reactor.TasteFor(picky, (uint)(i * 7919))).ToList();
            Assert.Equal(tastes, Enumerable.Range(1, 300).Select(i => reactor.TasteFor(picky, (uint)(i * 7919))).ToList());
            Assert.InRange(tastes.Count(t => t == Taste.Loves) / 300.0, 0.24, 0.44);
            Assert.InRange(tastes.Count(t => t == Taste.Ignores) / 300.0, 0.23, 0.43);
            uint loved = (uint)(7919 * (tastes.IndexOf(Taste.Loves) + 1)), ignored = (uint)(7919 * (tastes.IndexOf(Taste.Ignores) + 1));
            var love = reactor.React(picky, DemeanorTraits.None, CareKind.Feed, 1, loved);
            Assert.Equal((BehaviorAction.Call, BehaviorAction.Content), (love.First, love.Second));
            Assert.Equal(BehaviorAction.Snort, reactor.React(picky, DemeanorTraits.None, CareKind.Feed, 1, ignored).First);
        }

        [Fact]
        public void TwoPickyEatersCanDisagree()
        {
            var a = new Personality("Morgan", HorseOrigin.Stable, 1, HorseQuality.Good, new Traits(0, 0, 0, 0), new[] { Quirk.PickyEater }, new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal);
            var b = new Personality("Morgan", HorseOrigin.Stable, 2, HorseQuality.Good, new Traits(0, 0, 0, 0), new[] { Quirk.PickyEater }, new NaturalStats(0, 0, 0, 0), BreakingDifficulty.Normal);
            var r = Care();
            Assert.Contains(Enumerable.Range(1, 50), i => r.TasteFor(a, (uint)i * 31) != r.TasteFor(b, (uint)i * 31));
        }

        [Fact]
        public void TheCauseNamesTheStyleAndTheRoll()
        {
            var r = Care(0.2).React(Horse(1.5, 0), DemeanorTraits.None, CareKind.Brush, 2, 0);
            Assert.Contains("Grumpy Brush, bond 2: roll 0.200 vs", r.Cause);
        }
    }
}
