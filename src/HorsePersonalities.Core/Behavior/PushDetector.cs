namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// Turns the ridden horse's speed, stamina and the sprint key into "pushed" moments (decided Nov 26, 2026): spurring
    /// at low stamina, a long sprint, a hard stop or sharp turn. Each is reported once when it starts, not every check.
    /// </summary>
    public sealed class PushDetector
    {
        private readonly BehaviorTuning _t;
        private double _sprintSeconds;
        private bool _longSprintReported;
        private bool _lowStaminaSpurReported;
        private double _lastSpeed = -1;

        public PushDetector(BehaviorTuning tuning) { _t = tuning; }

        /// <param name="staminaCore">The horse's stamina core, 0-100.</param>
        public PushKind Update(double dtSeconds, bool mounted, double speedMs, double staminaCore, bool sprintPressed)
        {
            if (!mounted)
            {
                Reset();
                return PushKind.None;
            }
            PushKind pushed = PushKind.None;

            bool lowSpur = sprintPressed && staminaCore < _t.LowStamina;
            if (lowSpur && !_lowStaminaSpurReported) pushed |= PushKind.LowStaminaSpur;
            _lowStaminaSpurReported = lowSpur;

            _sprintSeconds = sprintPressed && speedMs >= _t.GallopSpeed ? _sprintSeconds + dtSeconds : 0;
            if (_sprintSeconds >= _t.LongSprintSeconds && !_longSprintReported)
            {
                pushed |= PushKind.LongSprint;
                _longSprintReported = true;
            }
            if (_sprintSeconds == 0) _longSprintReported = false;

            if (_lastSpeed >= _t.GallopSpeed && dtSeconds > 0 && (_lastSpeed - speedMs) / dtSeconds >= _t.HardStopSlowdown)
                pushed |= PushKind.HardStop;
            _lastSpeed = speedMs;
            return pushed;
        }

        public void Reset()
        {
            _sprintSeconds = 0;
            _longSprintReported = _lowStaminaSpurReported = false;
            _lastSpeed = -1;
        }
    }

    /// <summary>The player standing behind the horse long enough for a kick (Temper: "kicks if you linger behind it").</summary>
    public sealed class LingerTracker
    {
        private readonly BehaviorTuning _t;
        private double _seconds;
        private bool _reported;

        public LingerTracker(BehaviorTuning tuning) { _t = tuning; }

        /// <param name="behindDistance">How far the player is behind the horse along its back (negative: in front), metres.</param>
        /// <param name="sideDistance">How far the player is to the side of that line, metres.</param>
        public bool Update(double dtSeconds, bool mounted, double behindDistance, double sideDistance)
        {
            bool behind = !mounted && behindDistance > 0.3 && behindDistance <= _t.KickBehindDistance && System.Math.Abs(sideDistance) <= 1.0;
            if (!behind)
            {
                _seconds = 0;
                _reported = false;
                return false;
            }
            _seconds += dtSeconds;
            if (_seconds < _t.KickLingerSeconds || _reported) return false;
            _reported = true; // one chance per episode: step away and come back for another
            return true;
        }
    }
}
