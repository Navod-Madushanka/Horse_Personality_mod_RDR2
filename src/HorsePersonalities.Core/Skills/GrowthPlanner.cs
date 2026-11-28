using System;

namespace HorsePersonalities.Core.Skills
{
    /// <summary>
    /// Phase 8 (Build plan: Skills and learning speed, Long-use growth): how many ranks above or below the horse's own
    /// base rank its speed, acceleration and agility show. Pure; the game layer writes the result with the Phase 5
    /// base-rank machinery.
    /// </summary>
    public static class GrowthPlanner
    {
        /// <summary>
        /// Long-use ranks: 1 at Long use rank 1 bond and hours; community favorites get a 2nd at rank 2's bond and hours.
        /// Both a bond level and hours ridden are needed (a horse is only fully grown if well bonded and ridden a lot).
        /// </summary>
        public static int LongUseRanks(int bondLevel, double riddenHours, bool favorite, SkillTuning t)
        {
            if (t == null) return 0;
            int ranks = bondLevel >= t.LongUseRank1Bond && riddenHours >= t.LongUseRank1Hours ? 1 : 0;
            if (ranks == 1 && favorite && bondLevel >= t.LongUseRank2Bond && riddenHours >= t.LongUseRank2Hours) ranks = 2;
            return ranks;
        }

        /// <summary>
        /// Speed or acceleration: a positive natural stat shows once the skill reaches Skill rank at (it grows toward its
        /// ceiling); zero or negative shows from the start. Long-use ranks go on top. Any horse stops at its tier's up cap
        /// above the breed's usual value; a community favorite at Favorite max ranks, past the tier.
        /// </summary>
        public static int Offset(int natural, double skill, int longUseRanks, int tierUpCap, bool favorite, SkillTuning t)
        {
            int trained = natural > 0 ? (skill >= t.SkillRankAt ? natural : 0) : natural;
            int offset = trained + Math.Max(0, longUseRanks);
            int cap = favorite ? Math.Max(tierUpCap, t.FavoriteMaxRanks) : tierUpCap;
            return Math.Min(offset, cap);
        }

        /// <summary>Handling (experimental): the Agility ranks handling skill earns, within the tier's up cap.</summary>
        public static int AgilityOffset(double handlingSkill, int tierUpCap, SkillTuning t) =>
            t.HandlingGrowth && handlingSkill >= t.SkillRankAt ? Math.Min(t.HandlingAgilityRanks, tierUpCap) : 0;
    }
}
