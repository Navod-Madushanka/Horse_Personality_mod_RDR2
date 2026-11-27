using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>
    /// "Every behavior has a cooldown so nothing spams" (Build plan: Hard rules). Per horse: one cooldown per group,
    /// plus a shortest gap between any two behaviors. Time is in seconds of game time.
    /// </summary>
    public sealed class CooldownBook
    {
        private readonly BehaviorTuning _t;
        private readonly double _quirkCooldown;
        private readonly Dictionary<(int, BehaviorGroup), double> _readyAt = new Dictionary<(int, BehaviorGroup), double>();
        private readonly Dictionary<int, double> _gapEnds = new Dictionary<int, double>();

        /// <param name="quirkCooldownSeconds">The player's QuirkCooldownSeconds: shortest gap between two quirk events on one horse.</param>
        public CooldownBook(BehaviorTuning tuning, double quirkCooldownSeconds = 120)
        {
            _t = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _quirkCooldown = quirkCooldownSeconds;
        }

        public bool Ready(int horse, BehaviorGroup group, double now)
        {
            if (_gapEnds.TryGetValue(horse, out double gap) && now < gap && group != BehaviorGroup.Tell) return false;
            return !_readyAt.TryGetValue((horse, group), out double at) || now >= at;
        }

        public double Remaining(int horse, BehaviorGroup group, double now)
        {
            double r = _readyAt.TryGetValue((horse, group), out double at) ? at - now : 0;
            if (group != BehaviorGroup.Tell && _gapEnds.TryGetValue(horse, out double gap)) r = Math.Max(r, gap - now);
            return Math.Max(0, r);
        }

        public void Used(int horse, BehaviorGroup group, double now)
        {
            _readyAt[(horse, group)] = now + Length(group);
            if (group != BehaviorGroup.Tell) _gapEnds[horse] = now + _t.BehaviorGap;
        }

        private double Length(BehaviorGroup g)
        {
            switch (g)
            {
                case BehaviorGroup.Spook: return _t.SpookCooldown;
                case BehaviorGroup.Buck: return _t.BuckCooldown;
                case BehaviorGroup.Kick: return _t.KickCooldown;
                case BehaviorGroup.Quirk: return _quirkCooldown;
                case BehaviorGroup.Signature: return _t.SignatureCooldown;
                default: return _t.TellCooldown;
            }
        }
    }
}
