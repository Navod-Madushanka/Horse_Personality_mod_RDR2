using System;

namespace HorsePersonalities.Core.Behavior
{
    /// <summary>What scared the horse this check, strongest first when there are several.</summary>
    public enum Threat { None, RiderGunfire, Gunfire, Predator, Explosion }

    /// <summary>
    /// What the horse does. Only behaviors that passed Phase 1 (docs/phase-reports/phase-1-results.md):
    /// sounds, look-at, agitate, rear (horse action 1), quick stop (3), skid (8), throw rider (2), bolt (flee), kick (combat task).
    /// </summary>
    public enum BehaviorAction { None, Snort, LookAtThreat, Agitate, Rear, QuickStop, Skid, ThrowRider, Bolt, Kick }

    [Flags]
    public enum PushKind { None = 0, LowStaminaSpur = 1, LongSprint = 2, HardStop = 4 }

    /// <summary>Cooldown groups (Behavior tab: Spook, Buck, Kick, Tell cooldowns).</summary>
    public enum BehaviorGroup { Spook, Buck, Kick, Tell }

    /// <summary>One check's view of a horse, built by the game layer every 250 ms.</summary>
    public struct HorseSituation
    {
        /// <summary>The player is riding this horse.</summary>
        public bool Mounted;
        /// <summary>Not ridden, but the player is close by (the behaviors are meant to be seen).</summary>
        public bool PlayerNear;
        public double SpeedMs;
        public int GameHour;
        public int BondLevel;
        public Threat Threat;
        /// <summary>The threat's handle when it has one (a predator), for look-at and bolting away; 0 otherwise.</summary>
        public int ThreatEntity;
        public PushKind Pushed;
        /// <summary>The player has stood behind the horse for the kick linger time (one episode, reported once).</summary>
        public bool PlayerLingeredBehind;
    }

    /// <summary>A behavior to play, with the reason for the log (rule 9: the trait, the roll and the cooldown).</summary>
    public struct Decision
    {
        public BehaviorAction Action;
        public BehaviorGroup Group;
        public string Cause;

        public bool IsNone => Action == BehaviorAction.None;
        public static readonly Decision None = new Decision { Action = BehaviorAction.None, Cause = "" };
        public override string ToString() => IsNone ? "none" : $"{Action} ({Cause})";
    }
}
