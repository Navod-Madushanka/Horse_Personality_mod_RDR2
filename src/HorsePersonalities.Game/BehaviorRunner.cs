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
    /// Phases 6 and 7 in game: reads what the horse is doing and what is around it, asks Core (BehaviorBrain, QuirkBrain,
    /// CareReactor, BreakingDirector, BondPace) and plays the answer through the adapter. Every behavior is logged with its
    /// cause (rule 9). All of it is held in missions and cutscenes, except breaking.
    /// </summary>
    public sealed class BehaviorRunner
    {
        private const string Log = "Behavior";
        private const float NearDistance = 15f;
        private const int BreakingActive = 6; // eTamingState ATS_BREAKING_ACTIVE
        private const double FeedItemWindow = 5.0;
        private const double CalmAfterSpookWindow = 30.0;
        private static readonly uint InteractionBegin = Core.Hashing.Joaat.Hash("Interaction_Begin");
        private static readonly uint InteractionComplete = Core.Hashing.Joaat.Hash("Interaction_Complete");
        private static readonly uint InteractionCancel = Core.Hashing.Joaat.Hash("Interaction_Cancel");
        private static readonly uint InteractionBrush = Core.Hashing.Joaat.Hash("Interaction_Brush");
        private static readonly uint InteractionFood = Core.Hashing.Joaat.Hash("Interaction_Food");

        private readonly IGameApi _game;
        private readonly BreedCatalog _catalog;
        private readonly ModSettings _settings;
        private readonly HorseRegistry _registry;
        private readonly ILog _log;
        private readonly BehaviorTuning _t;
        private readonly BehaviorBrain _brain;
        private readonly QuirkBrain _quirks;
        private readonly CareReactor _care;
        private readonly CooldownBook _cooldowns;
        private readonly ThreatMemory _threats;
        private readonly PushDetector _push;
        private readonly LingerTracker _linger;
        private readonly Dictionary<int, BreakingDirector> _breaking = new Dictionary<int, BreakingDirector>();
        private readonly Dictionary<int, (int Rank, int Points)> _bondSeen = new Dictionary<int, (int, int)>();
        private readonly Dictionary<int, BondPace> _pace = new Dictionary<int, BondPace>();
        private readonly Dictionary<int, double> _stillSeconds = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _lastSpook = new Dictionary<int, double>();
        private readonly Dictionary<int, uint> _interactionType = new Dictionary<int, uint>();
        private readonly List<(double Due, string What, Action Run)> _scheduled = new List<(double, string, Action)>();
        private readonly IntervalGateSeconds _scan = new IntervalGateSeconds(0.5);
        private readonly Random _random = new Random();
        private string _lastHold;
        private bool _shotThisWindow, _explosionThisWindow;
        private int _explosionBy;
        private double _lastNow = -1;
        private double _now;
        private uint _lastItemRemoved;
        private double _lastItemRemovedAt = double.NegativeInfinity;
        private double _lastPlayerDistance = -1;
        private ScanResult _lastScan;

        private struct ScanResult
        {
            public int NewPredator, PasserBy, NpcBehind, Witnesses;
            public bool OtherHorseNear;
        }

        public BehaviorRunner(IGameApi game, BreedCatalog catalog, ModSettings settings, HorseRegistry registry, ILog log)
        {
            _game = game;
            _catalog = catalog;
            _settings = settings;
            _registry = registry;
            _log = log;
            _t = catalog.Behavior;
            Func<double> rng = () => _random.NextDouble();
            _brain = new BehaviorBrain(_t, settings, rng);
            _quirks = new QuirkBrain(_t, rng);
            _care = new CareReactor(_t, rng);
            _cooldowns = new CooldownBook(_t, settings.QuirkCooldownSeconds);
            _threats = new ThreatMemory(_t);
            _push = new PushDetector(_t);
            _linger = new LingerTracker(_t);
        }

        // ---- Every frame: events only show in the frame they happen ----

        public void OnEvents(IReadOnlyList<GameEvent> events)
        {
            int player = _game.PlayerPed;
            foreach (GameEvent e in events)
            {
                if (e.Hash == GameEvents.ShotFired.Hash || e.Hash == GameEvents.ShotFiredWhizzedBy.Hash) _shotThisWindow = true;
                else if (e.Hash == GameEvents.EntityExplosion.Hash)
                {
                    _explosionThisWindow = true;
                    _explosionBy = e.Data.Length > 0 ? (int)e.Data[0] : 0;
                }
                else if (e.Hash == GameEvents.InventoryItemRemoved.Hash && e.Data.Length >= 1)
                {
                    _lastItemRemoved = unchecked((uint)e.Data[0]);
                    _lastItemRemovedAt = _now;
                }
                else if (e.Hash == GameEvents.PedAnimalInteraction.Hash && e.Data.Length >= 3 && e.Data[0] == player)
                    OnInteraction((int)e.Data[1], unchecked((uint)e.Data[2]));
                else if (e.Hash == GameEvents.CalmPed.Hash && e.Data.Length >= 4 && e.Data[0] == player && e.Data[3] != 0)
                {
                    int horse = (int)e.Data[1];
                    if (_lastSpook.TryGetValue(horse, out double at) && _now - at <= CalmAfterSpookWindow) Care(horse, CareKind.Calm, 0);
                }
            }
        }

        /// <summary>Phase 1: a pat, brush or feed sends Interaction_Begin, its own type, then Complete and Cancel together.</summary>
        private void OnInteraction(int horse, uint type)
        {
            if (type == InteractionBegin || type == InteractionCancel) return;
            if (type != InteractionComplete)
            {
                _interactionType[horse] = type;
                return;
            }
            if (!_interactionType.TryGetValue(horse, out uint done)) return;
            _interactionType.Remove(horse);
            if (done == GameEvents.PatHash) Care(horse, CareKind.Pat, 0);
            else if (done == InteractionBrush) Care(horse, CareKind.Brush, 0);
            else if (done == InteractionFood) Care(horse, CareKind.Feed, _now - _lastItemRemovedAt <= FeedItemWindow ? _lastItemRemoved : 0u);
        }

        private void Care(int horse, CareKind kind, uint item)
        {
            if (_lastHold != null || !TryRecord(horse, out HorseRecord record, out HorseDescription d)) return;
            var (first, second, cause) = _care.React(record.Personality, d.Breed.DemeanorTraits, kind, d.BondRank, item);
            _log.Info(Log, $"{record.Id}: after {kind}: {(first == BehaviorAction.None ? "no reaction" : first + (second == BehaviorAction.None ? "" : " + " + second))} ({cause})");
            if (first == BehaviorAction.None) return;
            Schedule(_t.ReactionDelaySeconds, "care reaction", () => Play(horse, first, 0));
            if (second != BehaviorAction.None) Schedule(_t.ReactionDelaySeconds + 1.2, "care reaction", () => Play(horse, second, 0));
        }

        // ---- Every 250 ms ----

        /// <summary>The horse you ride, or your last horse if it is close by.</summary>
        public void Update(int mount, double nowSeconds)
        {
            _now = nowSeconds;
            double dt = _lastNow < 0 ? 0.25 : Math.Min(1.0, nowSeconds - _lastNow);
            _lastNow = nowSeconds;
            RunScheduled();
            bool shot = _shotThisWindow, explosion = _explosionThisWindow;
            _shotThisWindow = _explosionThisWindow = false;

            int player = _game.PlayerPed;
            bool mounted = mount != 0;
            int horse = mounted ? mount : _game.GetLastMount(player);
            if (horse == 0 || !_game.Exists(horse) || _game.IsDead(horse)) return;
            double playerDistance = mounted ? 0 : _game.DistanceBetween(horse, player);
            bool near = !mounted && playerDistance <= NearDistance;
            if (!mounted && !near) { _lastPlayerDistance = -1; return; }
            if (!TryRecord(horse, out HorseRecord record, out HorseDescription d)) return;
            Personality p = record.Personality;

            // Hard and Unbreakable wild horses while you break them. Not held by the mission guard: no mission breaks horses.
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
            if (hold != null) { _push.Reset(); return; }

            double speed = _game.GetSpeed(horse);
            double still = speed < _t.StandingStillSpeed ? (_stillSeconds.TryGetValue(horse, out double st) ? st : 0) + dt : 0;
            _stillSeconds[horse] = still;
            bool walkedAway = !mounted && _lastPlayerDistance >= 0 && _lastPlayerDistance < _t.ShadowTriggerDistance && playerDistance >= _t.ShadowTriggerDistance;
            _lastPlayerDistance = mounted ? -1 : playerDistance;
            if (_scan.Due(nowSeconds)) _lastScan = Scan(horse, player, nowSeconds);
            else _lastScan.NewPredator = 0; // a predator counts once, in the scan that found it

            // ---- Phase 6: threats, pushes, random spooks, kicks ----
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

            if (explosion && _threats.IsNew(horse, "explosion", nowSeconds)) { s.Threat = Threat.Explosion; s.ThreatEntity = _explosionBy; }
            else if (_lastScan.NewPredator != 0) { s.Threat = Threat.Predator; s.ThreatEntity = _lastScan.NewPredator; }
            else if (shot)
            {
                bool rider = _game.IsPedShooting(player);
                if (_threats.IsNew(horse, rider ? "rider gunfire" : "gunfire", nowSeconds)) s.Threat = rider ? Threat.RiderGunfire : Threat.Gunfire;
            }

            Decision decision = _brain.Decide(horse, p, s, dt, _cooldowns, nowSeconds);

            // ---- Phase 7: quirks and demeanor, when nothing bigger happened ----
            if (decision.IsNone)
            {
                var q = new QuirkSituation
                {
                    Mounted = mounted,
                    PlayerNear = near,
                    PlayerDistance = playerDistance,
                    StandingStillSeconds = still,
                    OtherHorseNear = _lastScan.OtherHorseNear,
                    PasserBy = _lastScan.PasserBy,
                    NpcBehind = _lastScan.NpcBehind,
                    Witnesses = _lastScan.Witnesses,
                    PlayerWalkedAway = walkedAway,
                    DirtRank = d.Breed.DemeanorTraits.Fussy > 0 ? _game.ReadAttribute(horse, PedAttribute.Dirtiness).Rank : 0,
                };
                decision = _quirks.Decide(horse, p, d.Breed.DemeanorTraits, q, dt, _cooldowns, nowSeconds);
            }
            if (decision.IsNone) return;

            if (decision.Group == BehaviorGroup.Spook) _lastSpook[horse] = nowSeconds;
            int target = decision.Target != 0 ? decision.Target : s.ThreatEntity != 0 ? s.ThreatEntity : player;
            Play(horse, decision.Action, target);
            _log.Info(Log, $"{record.Id}: {decision.Action} ({decision.Cause})");
        }

        /// <summary>One look at the peds around the horse every 0.5 s: predators, people passing, someone behind it, witnesses, other horses.</summary>
        private ScanResult Scan(int horse, int player, double now)
        {
            _threats.Forget(now);
            var r = new ScanResult();
            float range = (float)Math.Max(_t.PredatorRange, Math.Max(_t.NosyRange, _t.KickerWitnessRange));
            Vec3 at = _game.GetPosition(horse), forward = _game.GetForward(horse);
            int humans = 0;
            foreach (int ped in _game.PedsNear(horse, range))
            {
                if (ped == player || _game.IsDead(ped)) continue;
                uint model = _game.GetModel(ped);
                double dist = (_game.GetPosition(ped) - at).Length;
                if (_catalog.IsPredator(model))
                {
                    if (dist <= _t.PredatorRange && _threats.IsNew(horse, "predator " + ped, now) && r.NewPredator == 0) r.NewPredator = ped;
                    continue;
                }
                bool human = _game.IsHuman(ped);
                if (!human && _catalog.IsKnownModel(model) && dist <= _t.NosyRange) r.OtherHorseNear = true;
                if (dist <= _t.NosyRange && r.PasserBy == 0 && (human || _catalog.IsKnownModel(model))) r.PasserBy = ped;
                if (!human) continue;
                var (behind, side) = Vec3.BehindAndSide(at, forward, _game.GetPosition(ped));
                if (r.NpcBehind == 0 && behind > 0.3 && behind <= _t.KickBehindDistance && Math.Abs(side) <= 1.0) r.NpcBehind = ped;
                else if (dist <= _t.KickerWitnessRange) humans++;
            }
            r.Witnesses = humans;
            return r;
        }

        private bool TryRecord(int horse, out HorseRecord record, out HorseDescription d)
        {
            record = null;
            d = default;
            if (horse == 0 || !_game.Exists(horse)) return false;
            d = HorseDescriber.Describe(_game, _catalog, horse);
            if (d.Breed == null) return false;
            record = _registry.Find(horse, new HorseFingerprint(d.ModelName, d.IsMale));
            return record != null;
        }

        private void Play(int horse, BehaviorAction action, int target)
        {
            int player = _game.PlayerPed;
            switch (action)
            {
                case BehaviorAction.Snort: _game.PlayVocalization(horse, "SNORT"); break;
                case BehaviorAction.Call: _game.PlayVocalization(horse, "CALL"); break;
                case BehaviorAction.Content: _game.PlayVocalization(horse, "CONTENT"); break;
                case BehaviorAction.LookAt: _game.LookAtEntity(horse, target, 3000); break;
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
                case BehaviorAction.Bolt:
                    _game.FleeFrom(horse, target, 30f, 4000);
                    Schedule(4.5, "end bolt", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Kick:
                    _game.Combat(horse, player);
                    Schedule(1.5, "end kick", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.KickNpc:
                    _game.Combat(horse, target);
                    Schedule(1.5, "end kick", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Follow:
                    _game.FollowEntity(horse, player, 2.0f, (int)(_t.ShadowFollowSeconds * 1000));
                    Schedule(_t.ShadowFollowSeconds, "end follow", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Wander:
                    // Phase 1 trigger 41: wander, then graze with the grazing animation named.
                    _game.WanderNearby(horse, 15f);
                    Schedule(_t.WanderSeconds, "graze", () =>
                    {
                        _game.ClearTasks(horse);
                        _game.StartScenario(horse, "WORLD_ANIMAL_HORSE_GRAZING", (int)(_t.GrazeSeconds * 1000), "WORLD_ANIMAL_HORSE_GRAZING");
                    });
                    break;
            }
        }

        private void Schedule(double inSeconds, string what, Action run) => _scheduled.Add((_now + inSeconds, what, run));

        private void RunScheduled()
        {
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (_now < _scheduled[i].Due) continue;
                var item = _scheduled[i];
                _scheduled.RemoveAt(i);
                try { item.Run(); }
                catch (Exception ex) { _log.Error(Log, $"Scheduled {item.What} failed", ex); }
            }
        }

        /// <summary>
        /// Every second, for a permanent horse you ride: scales the bond XP the game just gave (Bond affinity, slower to
        /// bond 4 for good and prized horses, Hard wild horses) by adding or removing points.
        /// </summary>
        public void UpdateBonding(int horse)
        {
            if (!TryRecord(horse, out HorseRecord record, out _) || record.Status != PersonalityStatus.Permanent) return;
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
