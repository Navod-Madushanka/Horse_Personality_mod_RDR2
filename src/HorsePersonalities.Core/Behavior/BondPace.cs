using System;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// How fast a horse bonds (Build plan: Bonding and breaking). The game gives bond XP; the mod scales each gain by
    /// adding or removing points: Bond affinity (e^(slope x affinity)), the slower climb from bond 3 to 4 for good and
    /// prized horses, and slower for a wild horse that broke Hard. Fractions carry over to the next gain.
    /// </summary>
    public sealed class BondPace
    {
        private readonly BehaviorTuning _b;
        private readonly RollerTuning _r;
        private double _carry;

        public BondPace(BehaviorTuning behavior, RollerTuning roller)
        {
            _b = behavior;
            _r = roller;
        }

        public double Multiplier(Personality p, int bondLevel)
        {
            double m = Math.Exp(_b.BondAffinitySlope * p.Traits.BondAffinity);
            if (bondLevel == 3)
            {
                if (p.Prized) m *= _r.PrizedBond4Speed;
                else if (p.Quality == HorseQuality.Good) m *= _r.GoodBond4Speed;
            }
            if (p.Breaking == BreakingDifficulty.Hard) m *= _b.HardBondSpeed;
            return m;
        }

        /// <summary>
        /// Points to add (or, if negative, remove) after the game gave <paramref name="gained"/> points. Never removes
        /// more than was gained, and does nothing on a gain that raised the bond level (the level already moved).
        /// </summary>
        public int Adjustment(Personality p, int bondLevelBefore, int bondLevelAfter, int gained)
        {
            if (gained <= 0 || bondLevelAfter != bondLevelBefore)
            {
                _carry = 0;
                return 0;
            }
            double exact = gained * (Multiplier(p, bondLevelBefore) - 1) + _carry;
            int whole = (int)Math.Truncate(exact);
            _carry = exact - whole;
            return Math.Max(whole, -gained);
        }
    }
}
