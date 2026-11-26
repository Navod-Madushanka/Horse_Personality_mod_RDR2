using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// Hard and Unbreakable wild horses (Build plan: Bonding and breaking; Risks: the breaking minigame can't be
    /// controlled, so throws stand in). Fed every check while the player rides a wild horse:
    /// - Hard: during the first <c>Hard extra throws</c> breaking attempts, it throws you after <c>Hard throw after seconds</c>.
    /// - Unbreakable: every attempt throws you after <c>Unbreakable throw after seconds</c>, before the break can finish.
    /// One director per horse.
    /// </summary>
    public sealed class BreakingDirector
    {
        private readonly BehaviorTuning _t;
        private bool _breaking;
        private double _seconds;
        private bool _thrownThisAttempt;

        public int ExtraThrowsDone { get; private set; }

        public BreakingDirector(BehaviorTuning tuning) { _t = tuning; }

        /// <param name="breakingActive">Taming state BREAKING_ACTIVE (6) with the player on the horse.</param>
        /// <returns>True when the horse should throw the rider now.</returns>
        public bool Update(BreakingDifficulty difficulty, bool breakingActive, double dtSeconds, out string cause)
        {
            cause = null;
            if (!breakingActive)
            {
                _breaking = false;
                return false;
            }
            if (!_breaking)
            {
                _breaking = true;
                _seconds = 0;
                _thrownThisAttempt = false;
            }
            _seconds += dtSeconds;
            if (_thrownThisAttempt) return false;

            if (difficulty == BreakingDifficulty.Unbreakable && _seconds >= _t.UnbreakableThrowAfterSeconds)
            {
                _thrownThisAttempt = true;
                cause = $"Unbreakable: throws every attempt after {_t.UnbreakableThrowAfterSeconds:0.#} s";
                return true;
            }
            if (difficulty == BreakingDifficulty.Hard && ExtraThrowsDone < _t.HardExtraThrows && _seconds >= _t.HardThrowAfterSeconds)
            {
                _thrownThisAttempt = true;
                ExtraThrowsDone++;
                cause = $"Hard: extra throw {ExtraThrowsDone} of {_t.HardExtraThrows} after {_t.HardThrowAfterSeconds:0.#} s";
                return true;
            }
            return false;
        }
    }
}
