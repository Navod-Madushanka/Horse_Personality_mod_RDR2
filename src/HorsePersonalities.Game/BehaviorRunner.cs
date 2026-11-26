using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Behavior;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Tracking;
using PedAttribute = HorsePersonalities.Core.GameApi.PedAttribute;

namespace HorsePersonalities.Game
{
    /// <summary>
    /// Phase 6 in game: reads what the horse is doing and what is around it, asks the Core brain, and plays the behavior.
    /// All decisions and their causes live in Core (BehaviorBrain, PushDetector, BreakingDirector, BondPace); this class
    /// only gathers the inputs and calls the adapter.
    /// </summary>
    public sealed class BehaviorRunner
    {
        private const string Log = "Behavior";
        private const float NearDistance = 15f;
        private const int BreakingActive = 6; // eTamingState ATS_BREAKING_ACTIVE

        private readonly IGameApi _game;
        private readonly BreedCatalog _catalog;
        private readonly ModSettings _settings;
        private readonly HorseRegistry _registry;
        private readonly ILog _log;
        private readonly BehaviorTuning _t;
        private readonly BehaviorBrain _brain;
        private readonly CooldownBook _cooldowns;
        private readonly ThreatMemory _threats;
        private readonly PushDetector _push;
        private readonly LingerTracker _linger;
        private readonly Dictionary<int, BreakingDirector> _breaking = new Dictionary<int, BreakingDirector>();
        private readonly Dictionary<int, (int Rank, int Points)> _bondSeen = new Dictionary<int, (int, int)>();
        private readonly Dictionary<int, BondPace> _pace = new Dictionary<int, BondPace>();
        private readonly Random _random = new Random();
        private string _lastHold;
        private bool _shotThisWindow, _explosionThisWindow;
        private int _explosionBy;
        private double _lastNow = -1;
        private readonly List<(double Due, int Horse)> _clears = new List<(double, int)>();

        public BehaviorRunner(IGameApi game, BreedCatalog catalog, ModSettings settings, HorseRegistry registry, ILog log)
        {
            _game = game;
            _catalog = catalog;
            _settings = settings;
            _registry = registry;
            _log = log;
            _t = catalog.Behavior;
            _brain = new BehaviorBrain(_t, settings, () => _random.NextDouble());
            _cooldowns = new CooldownBook(_t);
            _threats = new ThreatMemory(_t);
            _push = new PushDetector(_t);
            _linger = new LingerTracker(_t);
        }

        /// <summary>Every frame: shots and explosions only show in that frame's events.</summary>
        public void OnEvents(IReadOnlyList<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                if (e.Hash == GameEvents.ShotFired.Hash || e.Hash == GameEvents.ShotFiredWhizzedBy.Hash) _shotThisWindow = true;
                else if (e.Hash == GameEvents.EntityExplosion.Hash)
                {
                    _explosionThisWindow = true;
                    _explosionBy = e.Data.Length > 0 ? (int)e.Data[0] : 0;
                }
            }
        }

        /// <summary>Every 250 ms: the horse you ride, or your last horse if it is close by.</summary>
        public void Update(int mount, double nowSeconds)
        {
            double dt = _lastNow < 0 ? 0.25 : Math.Min(1.0, nowSeconds - _lastNow);
            _lastNow = nowSeconds;
            for (int i = _clears.Count - 1; i >= 0; i--)
                if (nowSeconds >= _clears[i].Due) { _game.ClearTasks(_clears[i].Horse); _clears.RemoveAt(i); }
            bool shot = _shotThisWindow, explosion = _explosionThisWindow;
            _shotThisWindow = _explosionThisWindow = false;

            int player = _game.PlayerPed;
            bool mounted = mount != 0;
            int horse = mounted ? mount : _game.GetLastMount(player);
            if (horse == 0 || !_game.Exists(horse) || _game.IsDead(horse)) return;
            bool near = !mounted && _game.DistanceBetween(horse, player) <= NearDistance;
            if (!mounted && !near) return;

            HorseDescription d = HorseDescriber.Describe(_game, _catalog, horse);
            if (d.Breed == null) return;
            HorseRecord record = _registry.Find(horse, new HorseFingerprint(d.ModelName, d.IsMale));
            if (record == null) return;
            Personality p = record.Personality;

            // Hard and Unbreakable wild horses, while you break them. Not held by the mission guard: no mission breaks horses.
            if (mounted && p.Origin == HorseOrigin.Wild && p.Breaking != BreakingDifficulty.Normal)
            {
                if (!_breaking.TryGetValue(horse, out BreakingDirector director)) _breaking[horse] = director = new BreakingDirector(_t);
                if (director.Update(p.Breaking, _game.GetTamingState(horse) == BreakingActive, dt, out string why))
                {
                    _game.HorseAction(horse, 2); // throw rider (Phase 1: works)
                    _log.Info(Log, $"{record.Id}: ThrowRider ({why})");
                    return;
                }
            }

            string hold = MissionGuard.Holds(new GuardSignals
            {
                MissionFlag = _game.IsMissionActive,
                LetterBox = _game.HasLetterBox,
                AnimSceneCamera = _game.IsAnimSceneCamActive,
                GameplayCameraRendering = _game.IsGameplayCamRendering,
                PlayerControlOn = _game.IsPlayerControlOn,
            }, _settings.DisableDuringMissions);
            if (hold != _lastHold)
            {
                _log.Info(Log, hold == null ? "Behaviors back on." : $"Behaviors held: {hold}.");
                _lastHold = hold;
            }
            if (hold != null)
            {
                _push.Reset();
                return;
            }

            double speed = _game.GetSpeed(horse);
            var s = new HorseSituation
            {
                Mounted = mounted,
                PlayerNear = near,
                SpeedMs = speed,
                GameHour = _game.GameHour,
                BondLevel = d.BondRank,
                Pushed = _push.Update(dt, mounted, speed, _game.GetCoreValue(horse, AttributeCore.Stamina), mounted && _game.IsSprintPressed),
            };
            if (!mounted)
            {
                var (behind, side) = Vec3.BehindAndSide(_game.GetPosition(horse), _game.GetForward(horse), _game.GetPosition(player));
                s.PlayerLingeredBehind = _linger.Update(dt, false, behind, side);
            }
            else _linger.Update(dt, true, 0, 0);

            // The strongest new threat wins.
            if (explosion && _threats.IsNew(horse, "explosion", nowSeconds)) { s.Threat = Threat.Explosion; s.ThreatEntity = _explosionBy; }
            else
            {
                int predator = NewPredator(horse, nowSeconds);
                if (predator != 0) { s.Threat = Threat.Predator; s.ThreatEntity = predator; }
                else if (shot)
                {
                    bool rider = _game.IsPedShooting(player);
                    if (_threats.IsNew(horse, rider ? "rider gunfire" : "gunfire", nowSeconds)) s.Threat = rider ? Threat.RiderGunfire : Threat.Gunfire;
                }
            }

            Decision decision = _brain.Decide(horse, p, s, dt, _cooldowns, nowSeconds);
            if (decision.IsNone) return;
            Play(horse, decision.Action, s.ThreatEntity != 0 ? s.ThreatEntity : player);
            int clearMs = ClearAfterMs(decision.Action);
            if (clearMs > 0) _clears.Add((nowSeconds + clearMs / 1000.0, horse));
            _log.Info(Log, $"{record.Id}: {decision.Action} ({decision.Cause})");
        }

        private readonly IntervalGateSeconds _predatorScan = new IntervalGateSeconds(0.5);

        private int NewPredator(int horse, double now)
        {
            if (!_predatorScan.Due(now)) return 0;
            _threats.Forget(now);
            int found = 0;
            foreach (int ped in _game.PedsNear(horse, (float)_t.PredatorRange))
            {
                if (!_catalog.IsPredator(_game.GetModel(ped)) || _game.IsDead(ped)) continue;
                if (_threats.IsNew(horse, "predator " + ped, now) && found == 0) found = ped;
            }
            return found;
        }

        private void Play(int horse, BehaviorAction action, int target)
        {
            switch (action)
            {
                case BehaviorAction.Snort: _game.PlayVocalization(horse, "SNORT"); break;
                case BehaviorAction.LookAtThreat:
                    _game.LookAtEntity(horse, target, 3000);
                    _game.PlayVocalization(horse, "AGITATED");
                    break;
                case BehaviorAction.Agitate:
                    _game.HorseAgitate(horse, false);
                    _game.PlayVocalization(horse, "SNORT");
                    break;
                case BehaviorAction.Rear: _game.HorseAction(horse, 1); break;
                case BehaviorAction.QuickStop: _game.HorseAction(horse, 3); break;
                case BehaviorAction.Skid: _game.HorseAction(horse, 8); break;
                case BehaviorAction.ThrowRider: _game.HorseAction(horse, 2); break;
                case BehaviorAction.Bolt: _game.FleeFrom(horse, target, 30f, 4000); break;
                case BehaviorAction.Kick: _game.Combat(horse, _game.PlayerPed); break;
            }
        }

        /// <summary>Clears tasks a behavior started that don't end on their own (bolt, kick); called by the script on a timer.</summary>
        public static int ClearAfterMs(BehaviorAction action) =>
            action == BehaviorAction.Bolt ? 4500 : action == BehaviorAction.Kick ? 1500 : 0;

        /// <summary>
        /// Every second, for a permanent horse you ride: scales the bond XP the game just gave (Bond affinity, slower to
        /// bond 4 for good and prized horses, Hard wild horses) by adding or removing points.
        /// </summary>
        public void UpdateBonding(int horse)
        {
            if (horse == 0 || !_game.Exists(horse)) return;
            HorseDescription d = HorseDescriber.Describe(_game, _catalog, horse);
            if (d.Breed == null) return;
            HorseRecord record = _registry.Find(horse, new HorseFingerprint(d.ModelName, d.IsMale));
            if (record == null || record.Status != PersonalityStatus.Permanent) return;

            AttributeReading bond = _game.ReadAttribute(horse, PedAttribute.Bonding);
            if (!_bondSeen.TryGetValue(horse, out var before))
            {
                _bondSeen[horse] = (bond.Rank, bond.Points);
                return;
            }
            int gained = bond.Points - before.Points;
            if (!_pace.TryGetValue(horse, out BondPace pace)) _pace[horse] = pace = new BondPace(_t, _catalog.Roller);
            int adjust = pace.Adjustment(record.Personality, before.Rank, bond.Rank, gained);
            int points = bond.Points;
            if (adjust != 0)
            {
                points = Math.Max(before.Points, bond.Points + adjust);
                _game.SetAttributePoints(horse, PedAttribute.Bonding, points);
                _log.Debug(Log, $"{record.Id}: bond XP +{gained} scaled x{pace.Multiplier(record.Personality, before.Rank):0.00} -> {points - before.Points:+0;-0;0} (bond {bond.Rank})");
            }
            _bondSeen[horse] = (bond.Rank, points);
        }
    }

    /// <summary>An interval on the runner's own clock (seconds).</summary>
    internal sealed class IntervalGateSeconds
    {
        private readonly double _period;
        private double _last = double.NegativeInfinity;
        public IntervalGateSeconds(double period) { _period = period; }
        public bool Due(double now)
        {
            if (now - _last < _period) return false;
            _last = now;
            return true;
        }
    }
}
