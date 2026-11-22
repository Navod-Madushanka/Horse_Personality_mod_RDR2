using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Core.Safety
{
    /// <summary>
    /// "Never crash the game" (Rules for the coding agent, rule 8): runs each feature inside its own try/catch,
    /// logs failures, and turns off only the feature that keeps failing. Other features keep running.
    /// </summary>
    public sealed class FeatureGuard
    {
        private readonly ILog _log;
        private readonly int _maxFailures;
        private readonly Dictionary<string, int> _failures = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _disabled = new HashSet<string>(StringComparer.Ordinal);

        /// <param name="maxFailures">Failures allowed before a feature is switched off for the session.</param>
        public FeatureGuard(ILog log, int maxFailures = 3)
        {
            _log = log ?? NullLog.Instance;
            _maxFailures = maxFailures < 1 ? 1 : maxFailures;
        }

        public bool IsEnabled(string feature) => !_disabled.Contains(feature);

        public IEnumerable<string> DisabledFeatures => _disabled;

        /// <summary>Runs the action unless the feature is off. Returns true if it ran without throwing.</summary>
        public bool Run(string feature, Action action)
        {
            if (_disabled.Contains(feature)) return false;
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Fail(feature, ex);
                return false;
            }
        }

        /// <summary>Runs the function, returning <paramref name="fallback"/> if the feature is off or throws.</summary>
        public T Run<T>(string feature, Func<T> func, T fallback)
        {
            if (_disabled.Contains(feature)) return fallback;
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Fail(feature, ex);
                return fallback;
            }
        }

        private void Fail(string feature, Exception ex)
        {
            _failures.TryGetValue(feature, out int count);
            count++;
            _failures[feature] = count;
            _log.Error(feature, $"Failed ({count}/{_maxFailures})", ex);
            if (count >= _maxFailures)
            {
                _disabled.Add(feature);
                _log.Error(feature, "Turned off for this session after repeated failures. Other features keep running.");
            }
        }
    }
}
