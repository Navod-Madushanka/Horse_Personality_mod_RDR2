namespace HorsePersonalities.Core.Timing
{
    /// <summary>
    /// Runs slow checks on a fixed period from the per-frame tick, e.g. threats every 250 ms and bond level every
    /// 1000 ms (Build plan: Tick loop rules). Uses the game timer in milliseconds and survives timer wraparound.
    /// </summary>
    public sealed class IntervalGate
    {
        private readonly int _periodMs;
        private int _last;
        private bool _started;

        public IntervalGate(int periodMs)
        {
            _periodMs = periodMs < 0 ? 0 : periodMs;
        }

        public int PeriodMs => _periodMs;

        /// <summary>True at most once per period. The first call is always due.</summary>
        public bool Due(int nowMs)
        {
            if (!_started || unchecked(nowMs - _last) >= _periodMs)
            {
                _started = true;
                _last = nowMs;
                return true;
            }
            return false;
        }

        public void Reset() => _started = false;
    }

    /// <summary>Shortest gap between two firings of one behavior on one horse (Build plan: Hard rules).</summary>
    public sealed class Cooldown
    {
        private readonly int _durationMs;
        private int _readyAt;
        private bool _armed;

        public Cooldown(int durationMs)
        {
            _durationMs = durationMs < 0 ? 0 : durationMs;
        }

        public int DurationMs => _durationMs;

        public bool IsReady(int nowMs) => !_armed || unchecked(nowMs - _readyAt) >= 0;

        /// <summary>Milliseconds until ready; 0 when ready.</summary>
        public int RemainingMs(int nowMs) => IsReady(nowMs) ? 0 : unchecked(_readyAt - nowMs);

        public void Start(int nowMs)
        {
            _armed = true;
            _readyAt = unchecked(nowMs + _durationMs);
        }

        /// <summary>Starts the cooldown and returns true if it was ready; otherwise returns false and changes nothing.</summary>
        public bool TryStart(int nowMs)
        {
            if (!IsReady(nowMs)) return false;
            Start(nowMs);
            return true;
        }
    }
}
