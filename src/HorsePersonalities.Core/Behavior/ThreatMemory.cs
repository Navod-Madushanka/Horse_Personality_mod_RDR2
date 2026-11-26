using System.Collections.Generic;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// A horse judges a threat once (Behavior tab: Threat quiet seconds): each predator when it comes in range, gunfire
    /// once per gunfight. Without this a wolf standing nearby would get a fresh spook roll every check, and every horse,
    /// however brave, would spook in the end.
    /// </summary>
    public sealed class ThreatMemory
    {
        private readonly double _quiet;
        private readonly Dictionary<(int Horse, string Key), double> _lastSeen = new Dictionary<(int, string), double>();

        public ThreatMemory(BehaviorTuning tuning) { _quiet = tuning.ThreatQuietSeconds; }

        /// <summary>
        /// Call every time the threat is present. True only when it is new: never seen by this horse, or quiet for
        /// longer than Threat quiet seconds since it was last seen.
        /// </summary>
        public bool IsNew(int horse, string key, double now)
        {
            var k = (horse, key);
            bool isNew = !_lastSeen.TryGetValue(k, out double last) || now - last > _quiet;
            _lastSeen[k] = now;
            return isNew;
        }

        /// <summary>Drops memories older than the quiet time, so the table doesn't grow over a long session.</summary>
        public void Forget(double now)
        {
            var old = new List<(int, string)>();
            foreach (var kv in _lastSeen) if (now - kv.Value > _quiet) old.Add(kv.Key);
            foreach (var k in old) _lastSeen.Remove(k);
        }
    }
}
