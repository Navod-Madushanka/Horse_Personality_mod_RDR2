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
    /// Phases 6, 7 and 7b in game: reads what the horse is doing and what is around it, asks Core (BehaviorBrain, QuirkBrain,
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
        private readonly SignatureBrain _signatures;
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
        private bool _wasAiming;
        private bool _townChecked, _townValue;

        /// <summary>The town check, asked once per 250 ms check however many habits need it.</summary>
        private bool InTown(int horse)
        {
            if (!_townChecked) { _townValue = _game.IsInTown(horse); _townChecked = true; }
            return _townValue;
        }
        private int _lastMounted;
        private double _walkSeconds;
        private double _lastGallopAt = double.NegativeInfinity;
        private double _gallopSeconds, _lastGallopSeconds;
        private readonly Dictionary<int, double> _grudgeUntil = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _soothedAt = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _strictUntil = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _bribedUntil = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _minutesSinceTreat = new Dictionary<int, double>();
        private readonly Queue<(double At, double Heading)> _headings = new Queue<(double, double)>();
        // Phase 8: experience counting.
        private readonly Dictionary<int, double> _lastGunfightAt = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _lastSprintAt = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _minutesSinceSave = new Dictionary<int, double>();
        private bool _wasSprinting;
        private bool _spurWasDown;
        private readonly Queue<double> _spurPresses = new Queue<double>();
        private double _lastDangerAt = double.NegativeInfinity;
        private float _unhurriedRatio;
        private bool _unhurriedSnorted;
        private readonly HashSet<int> _silent = new HashSet<int>();
        private double _aimSeconds;
        private double _playerFiredAt = double.NegativeInfinity;
        private int _lazyHorse;
        private float _lazyRatio;
        /// <summary>Hill sigh placeholder until trigger 51-54 picks the best sigh sound.</summary>
        private const string SighSound = "SNORT";

        private struct ScanResult
        {
            public int NewPredator, PasserBy, NpcBehind;
            public bool OtherHorseNear;
            // Phase 7b
            public int TownPerson, StrangerClose, CreepTarget, EnemiesInCombat;
            public int OtherHorse, HogtiedPerson, GuardTarget;
            public int NearEnemy, LowerTierHorse, Crowd, HorseAhead, Challenger, RushingPerson;
            public int SensedPredator, CuriousTarget, CrowdPerson;
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
            _signatures = new SignatureBrain(_t, rng);
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
                else if (e.Hash == GameEvents.PedWhistle.Hash && _game.IsWhistlePressed) OnWhistle();
                else if (e.Hash == GameEvents.EntityDamaged.Hash && e.Data.Length >= 1 && e.Data[0] != 0 && _game.GetMount(player) == (int)e.Data[0]) OnHurt((int)e.Data[0]);
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

        /// <summary>Selective hearing: you whistled for your last horse while it's off on its own.</summary>
        private void OnWhistle()
        {
            int player = _game.PlayerPed;
            if (_lastHold != null || _game.GetMount(player) != 0) return;
            int horse = _game.GetLastMount(player);
            if (!TryRecord(horse, out HorseRecord record, out _)) return;
            Decision d = _signatures.OnWhistle(horse, record.Personality, false, _cooldowns, _now);
            if (d.IsNone) return;
            _log.Info(Log, $"{record.Id}: {d.Action} ({d.Cause})");
            Play(horse, d.Action, 0);
        }

        /// <summary>Grudge: the horse you ride was hurt (trigger 60 checks the damage event fires on a crash).</summary>
        private void OnHurt(int horse)
        {
            if (_lastHold != null || !TryRecord(horse, out HorseRecord record, out _)) return;
            bool already = (_grudgeUntil.TryGetValue(horse, out double until) && _now < until)
                || (_strictUntil.TryGetValue(horse, out double strictUntil) && _now < strictUntil);
            Decision d = _signatures.OnHurt(horse, record.Personality, _cooldowns, _now);
            if (d.IsNone) return;
            if (record.Personality.Has(Signature.Grudge)) _grudgeUntil[horse] = _now + _t.GrudgeSeconds;
            if (record.Personality.Has(Signature.Strict)) _strictUntil[horse] = _now + _t.StrictSeconds;
            if (already) return; // one sulk at a time: a second knock just makes it longer
            _log.Info(Log, $"{record.Id}: {d.Action} ({d.Cause})");
            Play(horse, d.Action, d.Action == BehaviorAction.Stare ? _game.PlayerPed : 0);
        }

        /// <summary>Jealous: the saddle horse sees you with another horse.</summary>
        private void CheckJealous(int other, string what)
        {
            int saddle = _game.GetSaddleHorse();
            if (saddle == 0 || saddle == other || !_game.Exists(saddle) || !TryRecord(saddle, out HorseRecord record, out _)) return;
            Decision d = _signatures.OnRival(saddle, record.Personality, _game.DistanceBetween(saddle, other), what, _cooldowns, _now);
            if (d.IsNone) return;
            _log.Info(Log, $"{record.Id}: {d.Action} ({d.Cause})");
            Play(saddle, d.Action, other);
        }

        /// <summary>Phase 1: a pat, brush or feed sends Interaction_Begin, its own type, then Complete and Cancel together.</summary>
        private void OnInteraction(int horse, uint type)
        {
            if (type == InteractionBegin || type == InteractionCancel) return;
            if (type != InteractionComplete)
            {
                _interactionType[horse] = type;
                if (type == InteractionBrush) BrushStarted(horse);
                return;
            }
            if (!_interactionType.TryGetValue(horse, out uint done)) return;
            _interactionType.Remove(horse);
            if (done == GameEvents.PatHash) Care(horse, CareKind.Pat, 0);
            else if (done == InteractionBrush) Care(horse, CareKind.Brush, 0);
            else if (done == InteractionFood) Care(horse, CareKind.Feed, _now - _lastItemRemovedAt <= FeedItemWindow ? _lastItemRemoved : 0u);
        }

        /// <summary>Brush excitement (coat signature): reacts as the brushing starts, not after.</summary>
        private void BrushStarted(int horse)
        {
            if (_lastHold != null || !TryRecord(horse, out HorseRecord record, out _)) return;
            var (first, second, cause) = _care.BrushStarted(record.Personality);
            if (cause.Length == 0) return;
            _log.Info(Log, $"{record.Id}: brush started: {(first == BehaviorAction.None ? "nothing" : first + " + " + second)} ({cause})");
            if (first == BehaviorAction.None) return;
            Play(horse, first, 0);
            Schedule(1.0, "brush excitement", () => Play(horse, second, 0));
        }

        /// <summary>The coat design the horse was rolled with (Phase 7b), or null: older rolls and test presets have none.</summary>
        private Coat CoatOf(Personality p, HorseDescription d) =>
            p.Archetype != null && _catalog.TryGetCoat(d.ModelName, out Coat c) ? c : null;

        private DemeanorTraits DemeanorOf(Personality p, HorseDescription d) => CoatOf(p, d)?.Demeanor ?? d.Breed.DemeanorTraits;

        private void Care(int horse, CareKind kind, uint item)
        {
            if (_lastHold != null || !TryRecord(horse, out HorseRecord record, out HorseDescription d)) return;
            if (kind != CareKind.Calm) CheckJealous(horse, kind == CareKind.Pat ? "patted" : kind == CareKind.Brush ? "brushed" : "fed");
            if (kind == CareKind.Pat || kind == CareKind.Calm) _soothedAt[horse] = _now; // Needs soothing
            bool sulking = _grudgeUntil.TryGetValue(horse, out double grudge) && _now < grudge;
            if (sulking && kind == CareKind.Feed)
            {
                _grudgeUntil.Remove(horse);
                sulking = false;
                _log.Info(Log, $"{record.Id}: Grudge: forgiven for a treat");
            }
            var (first, second, cause) = _care.React(record.Personality, DemeanorOf(record.Personality, d), kind, d.BondRank, item,
                CoatOf(record.Personality, d), _catalog.TreatName(item), sulking,
                record.Personality.Has(Signature.CrowdShy) && _game.GetClosestHumanPed(horse, (float)_t.CrowdShyStrangerRange, _game.PlayerPed) != 0);
            // Bribable: a treat it loves buys a couple of minutes of sprinting uphill.
            string fed = _catalog.TreatName(item);
            Coat coatFed = CoatOf(record.Personality, d);
            // Treat tantrum: a treat it loves ends it; so does one the mod can't name yet (no item code in treats.csv).
            if (kind == CareKind.Feed && record.Personality.Has(Signature.TreatTantrum)
                && (fed == null || coatFed == null || System.Linq.Enumerable.Any(coatFed.LovesTreats, t => string.Equals(t, fed, StringComparison.OrdinalIgnoreCase))))
                _minutesSinceTreat[horse] = 0;
            if (kind == CareKind.Feed && record.Personality.Has(Signature.Bribable) && fed != null && coatFed != null
                && System.Linq.Enumerable.Any(coatFed.LovesTreats, t => string.Equals(t, fed, StringComparison.OrdinalIgnoreCase)))
            {
                _bribedUntil[horse] = _now + _t.BribeSeconds;
                _log.Info(Log, $"{record.Id}: Bribable: {fed} buys {_t.BribeSeconds:0} s of hill sprinting");
            }
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
            _townChecked = false; // Phase 9: the map-zone lookup runs at most once per check
            bool shot = _shotThisWindow, explosion = _explosionThisWindow;
            _shotThisWindow = _explosionThisWindow = false;

            int player = _game.PlayerPed;
            bool mounted = mount != 0;
            int horse = mounted ? mount : _game.GetLastMount(player);
            _lazyHorse = 0;
            if (horse == 0 || !_game.Exists(horse) || _game.IsDead(horse)) return;
            bool justMounted = mounted && horse != _lastMounted;
            if (!mounted || horse != _lastMounted) _unhurriedSnorted = false;
            _lastMounted = mounted ? horse : 0;
            double playerDistance = mounted ? 0 : _game.DistanceBetween(horse, player);
            bool near = !mounted && playerDistance <= NearDistance;
            if (!TryRecord(horse, out HorseRecord record, out HorseDescription d)) return;
            Personality p = record.Personality;
            // Far away, only Escape artist and Wanderlust are watched (they need you gone).
            bool far = !mounted && !near;
            if (far) _lastPlayerDistance = -1;
            if (far && !SignatureBrain.WatchedFar(p)) return;

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
            if (hold != null) { _push.Reset(); _wasAiming = false; _aimSeconds = 0; return; }
            if (p.Has(Signature.SilentStealth)) _silent.Add(horse); else _silent.Remove(horse);
            if (justMounted)
            {
                CheckJealous(horse, "rode");
                Decision nip = _signatures.OnMounted(horse, p, _cooldowns, nowSeconds);
                if (!nip.IsNone) { Play(horse, nip.Action, player); _log.Info(Log, $"{record.Id}: {nip.Action} ({nip.Cause})"); return; }
            }

            double speed = _game.GetSpeed(horse);
            bool walkingNow = mounted && speed >= _t.StandingStillSpeed && speed < _t.WalkSpeed;
            _walkSeconds = walkingNow ? _walkSeconds + dt : 0;
            if (mounted && speed >= _t.GallopSpeed)
            {
                _lastGallopAt = nowSeconds;
                _gallopSeconds += dt;
                _lastGallopSeconds = _gallopSeconds;
            }
            else _gallopSeconds = 0;
            double still = speed < _t.StandingStillSpeed ? (_stillSeconds.TryGetValue(horse, out double st) ? st : 0) + dt : 0;
            _stillSeconds[horse] = still;
            bool walkedAway = !mounted && _lastPlayerDistance >= 0 && _lastPlayerDistance < _t.ShadowTriggerDistance && playerDistance >= _t.ShadowTriggerDistance;
            _lastPlayerDistance = mounted || far ? -1 : playerDistance;
            if (far)
            {
                if (p.Has(Signature.GuardDog) && _scan.Due(nowSeconds)) _lastScan = Scan(horse, player, p, nowSeconds, d.Breed);
                var away = new SignatureSituation { PlayerDistance = playerDistance, StandingStillSeconds = still, GameHour = _game.GameHour, BondLevel = d.BondRank,
                    GuardTarget = _lastScan.GuardTarget };
                Decision gone = _signatures.Decide(horse, p, away, dt, _cooldowns, nowSeconds);
                if (!gone.IsNone) { Play(horse, gone.Action, 0); _log.Info(Log, $"{record.Id}: {gone.Action} ({gone.Cause})"); }
                return;
            }
            if (_scan.Due(nowSeconds)) _lastScan = Scan(horse, player, p, nowSeconds, d.Breed);
            else _lastScan.NewPredator = 0; // a predator counts once, in the scan that found it
            CountExperience(horse, record, mounted, speed, dt, shot || explosion, _lastScan.NewPredator != 0, nowSeconds);
            Core.Skills.SkillSet skills = _catalog.Skills != null ? Core.Skills.SkillSet.From(record.Experience, d.Breed.Learning, _catalog.Skills) : default;
            double bonus = _catalog.Skills?.SkillCourageBonus ?? 0;

            // ---- Phase 6: threats, pushes, random spooks, kicks ----
            var s = new HorseSituation
            {
                Mounted = mounted,
                PlayerNear = near,
                SpeedMs = speed,
                GameHour = _game.GameHour,
                BondLevel = d.BondRank,
                InTown = p.Has(Signature.CityLover) && InTown(horse),
                InFight = _lastScan.EnemiesInCombat > 0,
                GunfireSkillCourage = skills.Gunfire * bonus,
                PredatorSkillCourage = skills.Predators * bonus,
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
                if (rider) _playerFiredAt = nowSeconds;
                if (_threats.IsNew(horse, rider ? "rider gunfire" : "gunfire", nowSeconds)) s.Threat = rider ? Threat.RiderGunfire : Threat.Gunfire;
            }

            if (s.Threat != Threat.None || shot || explosion || _lastScan.EnemiesInCombat > 0) _lastDangerAt = nowSeconds;
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
                    PlayerWalkedAway = walkedAway,
                    DirtRank = DemeanorOf(p, d).Fussy > 0 ? _game.ReadAttribute(horse, PedAttribute.Dirtiness).Rank : 0,
                };
                decision = _quirks.Decide(horse, p, DemeanorOf(p, d), q, dt, _cooldowns, nowSeconds);
            }

            // ---- Phase 7b: coat signatures ----
            bool aiming = mounted && (p.Has(Signature.DramaticRear) || p.Has(Signature.BoredBuck)) && _game.IsPlayerAiming;
            bool startedAiming = aiming && !_wasAiming;
            _wasAiming = aiming;
            _aimSeconds = aiming ? _aimSeconds + dt : 0;
            bool spurring = mounted && _game.IsSprintPressed;
            bool bribedNow = _bribedUntil.TryGetValue(horse, out double bribedTill) && nowSeconds < bribedTill;
            double? cap = _signatures.LazyCap(p, mounted, spurring, bribedNow);
            double? hurry = _signatures.UnhurriedCap(p, mounted, spurring, nowSeconds - _lastDangerAt, bribedNow)
                ?? _signatures.StrictCap(p, mounted, spurring, _strictUntil.TryGetValue(horse, out double strict) && nowSeconds < strict);
            double climb = p.Has(Signature.HillSigh) || p.Has(Signature.DownhillSnob) || p.Has(Signature.HillCrawler) || p.Has(Signature.Balker) || p.Has(Signature.CliffNerves) ? _game.GetVelocity(horse).Z : 0;
            double? crawl = _signatures.HillCap(p, mounted, climb, _bribedUntil.TryGetValue(horse, out double bribe) && nowSeconds < bribe);
            double? dawdle = _signatures.TownCap(p, mounted, p.Has(Signature.TownDawdler) && mounted && InTown(horse));
            _lazyRatio = dawdle.HasValue ? (float)dawdle.Value : crawl.HasValue ? (float)crawl.Value : cap.HasValue ? (float)cap.Value : 0f;
            _unhurriedRatio = dawdle.HasValue ? (float)dawdle.Value : crawl.HasValue ? (float)crawl.Value : hurry.HasValue ? (float)hurry.Value : 0f;
            if (cap.HasValue || crawl.HasValue || dawdle.HasValue || p.Has(Signature.Unhurried) || p.Has(Signature.Strict)) _lazyHorse = horse;
            if (hurry.HasValue && !_unhurriedSnorted)
            {
                _unhurriedSnorted = true;
                Vocal(horse, "SNORT");
                _log.Info(Log, $"{record.Id}: Unhurried: no danger for {Math.Min(9999, nowSeconds - _lastDangerAt):0} s, so it trots");
            }
            if (decision.IsNone && p.Signatures.Count > 0)
            {
                bool town = (p.Has(Signature.TownDisdain) || p.Has(Signature.CountryBumpkin)) && InTown(horse);
                var sig = new SignatureSituation
                {
                    Mounted = mounted,
                    PlayerNear = near,
                    PlayerDistance = playerDistance,
                    SpeedMs = speed,
                    StandingStillSeconds = still,
                    GameHour = s.GameHour,
                    BondLevel = d.BondRank,
                    InTown = town,
                    TownPerson = town && p.Has(Signature.TownDisdain) ? _lastScan.TownPerson : 0,
                    StartedAiming = startedAiming,
                    EnemiesInCombat = _lastScan.EnemiesInCombat,
                    PlayerFiredRecently = nowSeconds - _playerFiredAt < 30,
                    ClimbSpeed = climb,
                    SecondsSinceDanger = nowSeconds - _lastDangerAt,
                    SoothedSinceDanger = _soothedAt.TryGetValue(horse, out double soothed) && soothed >= _lastDangerAt,
                    SensedPredator = _lastScan.SensedPredator,
                    CuriousTarget = _lastScan.CuriousTarget,
                    CrowdPerson = _lastScan.CrowdPerson,
                    RecentSpurPresses = SpurPresses(mounted && p.Has(Signature.SpurSnob), nowSeconds),
                    MinutesSinceTreat = TreatClock(horse, mounted && p.Has(Signature.TreatTantrum), dt),
                    TurnedDegrees = TurnClock(horse, mounted && p.Has(Signature.TurnGrumble), nowSeconds),
                    GallopingSeconds = _gallopSeconds,
                    InWater = (p.Has(Signature.WaterSigh) || p.Has(Signature.Balker)) && mounted && _game.IsInWater(horse),
                    DirtRank = p.Has(Signature.MudTantrum) ? _game.ReadAttribute(horse, PedAttribute.Dirtiness).Rank : 0,
                    StrangerClose = _lastScan.StrangerClose,
                    CreepTarget = _lastScan.CreepTarget,
                    AimingSeconds = _aimSeconds,
                    OtherHorse = _lastScan.OtherHorse,
                    HogtiedPerson = _lastScan.HogtiedPerson,
                    GuardTarget = _lastScan.GuardTarget,
                    WalkingSeconds = _walkSeconds,
                    NearEnemy = _lastScan.NearEnemy,
                    LowerTierHorse = _lastScan.LowerTierHorse,
                    JustStoppedFromGallop = mounted && still > 0 && nowSeconds - _lastGallopAt <= 3,
                    Crowd = _lastScan.Crowd,
                    HorseAhead = _lastScan.HorseAhead,
                    Challenger = _lastScan.Challenger,
                    RushingPerson = _lastScan.RushingPerson,
                    SecondsSinceGallop = mounted && speed >= _t.GallopSpeed ? 0 : nowSeconds - _lastGallopAt,
                    LastGallopSeconds = _lastGallopSeconds,
                };
                decision = _signatures.Decide(horse, p, sig, dt, _cooldowns, nowSeconds);
            }
            if (decision.IsNone) return;

            if (decision.Group == BehaviorGroup.Spook) _lastSpook[horse] = nowSeconds;
            int target = decision.Target != 0 ? decision.Target : s.ThreatEntity != 0 ? s.ThreatEntity : player;
            Play(horse, decision.Action, target);
            _log.Info(Log, $"{record.Id}: {decision.Action} ({decision.Cause})");
        }

        /// <summary>
        /// Phase 8: what this horse lives through, for a permanent horse: minutes ridden and galloping, sprints, gunfights
        /// (at most one per Gunfight gap) and predators. Held in missions with everything else (the caller returns first).
        /// Saved on each gunfight or predator, and every Save every ridden minutes of riding (rule 7: events, not frames).
        /// </summary>
        private void CountExperience(int horse, HorseRecord record, bool mounted, double speed, double dt, bool gunfire, bool predator, double now)
        {
            Core.Skills.SkillTuning st = _catalog.Skills;
            if (st == null || record.Status != PersonalityStatus.Permanent) { _wasSprinting = false; return; }
            double k = _settings.ExperienceSpeed;
            if (k <= 0) return;
            Core.Skills.Experience e = record.Experience;
            if (mounted)
            {
                double minutes = dt / 60.0 * k;
                e.RiddenMinutes += minutes;
                if (speed >= _t.GallopSpeed) e.GallopMinutes += minutes;
                double since = (_minutesSinceSave.TryGetValue(horse, out double m) ? m : 0) + minutes;
                if (since >= st.SaveEveryRiddenMinutes) { _registry.ExperienceChanged(record); since = 0; }
                _minutesSinceSave[horse] = since;

                bool sprinting = _game.IsSprintPressed;
                if (sprinting && !_wasSprinting && speed < _t.GallopSpeed
                    && (!_lastSprintAt.TryGetValue(horse, out double ls) || now - ls >= st.SprintGapSeconds))
                {
                    e.Sprints += k;
                    _lastSprintAt[horse] = now;
                }
                _wasSprinting = sprinting;
            }
            else _wasSprinting = false;
            if (gunfire && (!_lastGunfightAt.TryGetValue(horse, out double lg) || now - lg >= st.GunfightGapSeconds))
            {
                e.Gunfights += k;
                _lastGunfightAt[horse] = now;
                _registry.ExperienceChanged(record);
                _log.Info(Log, $"{record.Id}: gunfight counted ({e.Gunfights:0.#} so far)");
            }
            if (predator)
            {
                e.Predators += k;
                _registry.ExperienceChanged(record);
                _log.Info(Log, $"{record.Id}: predator encounter counted ({e.Predators:0.#} so far)");
            }
        }

        /// <summary>Turn grumble: how far the horse's heading has swung over the last Turn grumble seconds (0 to 180).</summary>
        private double TurnClock(int horse, bool watching, double now)
        {
            if (!watching) { _headings.Clear(); return 0; }
            Vec3 f = _game.GetForward(horse);
            double heading = Math.Atan2(f.Y, f.X) * 180 / Math.PI;
            _headings.Enqueue((now, heading));
            while (_headings.Count > 0 && now - _headings.Peek().At > _t.TurnGrumbleSeconds) _headings.Dequeue();
            double most = 0;
            foreach (var (_, h) in _headings)
            {
                double d = Math.Abs(heading - h) % 360;
                most = Math.Max(most, d > 180 ? 360 - d : d);
            }
            return most;
        }

        /// <summary>Treat tantrum: minutes ridden since its last treat (this session).</summary>
        private double TreatClock(int horse, bool ticking, double dt)
        {
            double m = _minutesSinceTreat.TryGetValue(horse, out double v) ? v : 0;
            if (ticking) _minutesSinceTreat[horse] = m += dt / 60.0;
            return m;
        }

        /// <summary>Spur snob: sprint presses (rising edges) in the last Spur spam seconds. Read every 250 ms, so a very fast double tap counts once.</summary>
        private int SpurPresses(bool watch, double now)
        {
            if (!watch) { _spurPresses.Clear(); _spurWasDown = false; return 0; }
            bool down = _game.IsSprintPressed;
            if (down && !_spurWasDown) _spurPresses.Enqueue(now);
            _spurWasDown = down;
            while (_spurPresses.Count > 0 && now - _spurPresses.Peek() > _t.SpurSpamSeconds) _spurPresses.Dequeue();
            return _spurPresses.Count;
        }

        /// <summary>One look at the peds around the horse every 0.5 s: predators, people passing, someone behind it, other horses.</summary>
        private ScanResult Scan(int horse, int player, Personality p, double now, Breed own)
        {
            _threats.Forget(now);
            var r = new ScanResult();
            bool snakeDrama = p.Has(Signature.SnakeDrama);
            double snakeRange = _t.PredatorRange * (snakeDrama ? _t.SnakeRangeMultiplier : 1);
            float range = (float)Math.Max(Math.Max(_t.PredatorRange, snakeRange), (float)_t.NosyRange);
            bool fights = p.Has(Signature.Prankster) || p.Has(Signature.Unhurried) || p.Has(Signature.WarHorse) || p.Has(Signature.NeedsSoothing);
            if (fights) range = Math.Max(range, (float)_t.PranksterEnemyRange);
            if (p.Has(Signature.ShortKing) || p.Has(Signature.Snob)) range = Math.Max(range, (float)_t.ShortKingRange);
            if (p.Has(Signature.Showboat)) range = Math.Max(range, (float)_t.ShowboatRange);
            if (p.Has(Signature.MustLead)) range = Math.Max(range, (float)_t.MustLeadRange);
            if (p.Has(Signature.Racer)) range = Math.Max(range, (float)_t.RacerRange);
            if (p.Has(Signature.SixthSense)) range = Math.Max(range, (float)_t.SixthSenseRange);
            if (p.Has(Signature.Curious)) range = Math.Max(range, (float)_t.CuriousRange);
            Vec3 me = _game.GetPosition(player);
            if (p.Has(Signature.CampCreeper)) range = Math.Max(range, (float)_t.CreeperRange);
            Vec3 at = _game.GetPosition(horse), forward = _game.GetForward(horse);
            foreach (int ped in _game.PedsNear(horse, range))
            {
                if (ped == player || _game.IsDead(ped)) continue;
                uint model = _game.GetModel(ped);
                double dist = (_game.GetPosition(ped) - at).Length;
                if (_catalog.IsPredator(model))
                {
                    double reach = _catalog.IsSnake(model) ? snakeRange : _t.PredatorRange;
                    if (dist <= reach && _threats.IsNew(horse, "predator " + ped, now) && r.NewPredator == 0) r.NewPredator = ped;
                    else if (p.Has(Signature.SixthSense) && dist > reach && dist <= _t.SixthSenseRange && r.SensedPredator == 0
                        && _threats.IsNew(horse, "sensed " + ped, now)) r.SensedPredator = ped;
                    if (p.Has(Signature.Curious) && dist <= _t.CuriousRange && r.CuriousTarget == 0) r.CuriousTarget = ped;
                    continue;
                }
                bool human = _game.IsHuman(ped);
                if (human)
                {
                    if (r.TownPerson == 0 && dist <= _t.TownDisdainRange && p.Has(Signature.TownDisdain)) r.TownPerson = ped;
                    if (r.StrangerClose == 0 && dist <= _t.SoulStareRange && p.Has(Signature.SoulStare)) r.StrangerClose = ped;
                    if (r.CreepTarget == 0 && dist <= _t.CreeperRange && p.Has(Signature.CampCreeper)) r.CreepTarget = ped;
                    if (dist <= _t.PranksterEnemyRange && fights && _game.IsPedInCombatWith(ped, player))
                    {
                        r.EnemiesInCombat++;
                        if (r.NearEnemy == 0 && dist <= _t.WarHorseRange && p.Has(Signature.WarHorse)) r.NearEnemy = ped;
                    }
                    if (dist <= _t.ShowboatRange && p.Has(Signature.Showboat)) r.Crowd++;
                    if (r.HogtiedPerson == 0 && dist <= _t.BountyStomperRange && p.Has(Signature.BountyStomper) && _game.IsHogtied(ped)) r.HogtiedPerson = ped;
                    if (r.GuardTarget == 0 && dist <= _t.GuardRange && p.Has(Signature.GuardDog)) r.GuardTarget = ped;
                    if (r.CrowdPerson == 0 && dist <= _t.CrowdShyRange && p.Has(Signature.CrowdShy)) r.CrowdPerson = ped;
                    if (r.RushingPerson == 0 && p.Has(Signature.StrangerDanger))
                    {
                        Vec3 at2 = _game.GetPosition(ped), toMe = me - at2;
                        double gap = toMe.Length;
                        if (gap <= _t.StrangerDangerRange && gap > 0.1)
                        {
                            Vec3 v = _game.GetVelocity(ped);
                            double closing = (v.X * toMe.X + v.Y * toMe.Y) / gap;
                            if (closing >= _t.StrangerDangerSpeed) r.RushingPerson = ped;
                        }
                    }
                }
                else if (_catalog.IsKnownModel(model))
                {
                    if (r.OtherHorse == 0 && p.Has(Signature.ShortKing) && dist <= _t.ShortKingRange) r.OtherHorse = ped;
                    if (r.LowerTierHorse == 0 && p.Has(Signature.Snob) && dist <= _t.ShortKingRange
                        && _catalog.TryGetBreedByModel(model, out Breed other) && other.Tier <= own.Tier && other.Name != own.Name) r.LowerTierHorse = ped;
                    if (r.Challenger == 0 && p.Has(Signature.Racer) && dist <= _t.RacerRange && _game.GetSpeed(ped) >= _t.WalkSpeed) r.Challenger = ped;
                    if (r.HorseAhead == 0 && p.Has(Signature.MustLead) && dist <= _t.MustLeadRange)
                    {
                        var (behindIt, sideIt) = Vec3.BehindAndSide(at, forward, _game.GetPosition(ped));
                        if (behindIt < -1.0 && Math.Abs(sideIt) <= 4.0 && _game.GetSpeed(ped) >= _t.WalkSpeed) r.HorseAhead = ped;
                    }
                }
                if (!human && _catalog.IsKnownModel(model) && dist <= _t.NosyRange) r.OtherHorseNear = true;
                if (dist <= _t.NosyRange && r.PasserBy == 0 && (human || _catalog.IsKnownModel(model))) r.PasserBy = ped;
                bool dog = !human && _catalog.IsDog(model);
                if (!human && !dog) continue;
                var (behind, side) = Vec3.BehindAndSide(at, forward, _game.GetPosition(ped));
                // Kicker: a person or a dog right behind it (Navod, Nov 28: dogs too).
                if (r.NpcBehind == 0 && behind > 0.3 && behind <= _t.KickBehindDistance && Math.Abs(side) <= 1.0) r.NpcBehind = ped;
            }
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
                case BehaviorAction.Snort: Vocal(horse, "SNORT"); break;
                case BehaviorAction.Call: Vocal(horse, "CALL"); break;
                case BehaviorAction.Content: Vocal(horse, "CONTENT"); break;
                case BehaviorAction.LookAt: _game.LookAtEntity(horse, target, 3000); break;
                case BehaviorAction.LookAtThreat:
                    _game.LookAtEntity(horse, target, 3000);
                    Vocal(horse, "AGITATED");
                    break;
                case BehaviorAction.Agitate:
                    _game.HorseAgitate(horse, false);
                    Vocal(horse, "SNORT");
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
                // ---- Phase 7b ----
                case BehaviorAction.Snub:
                    Vocal(horse, "SNORT");
                    if (target != 0 && target != player) _game.LookAtEntity(horse, target, 3000);
                    break;
                case BehaviorAction.Sigh: Vocal(horse, SighSound); break;
                case BehaviorAction.Stare: _game.LookAtEntity(horse, target, (int)(_t.SoulStareSeconds * 1000)); break;
                case BehaviorAction.Brawl:
                    _game.Combat(horse, target);
                    Schedule(_t.BrawlSeconds, "end brawl", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Creep:
                    _game.FollowEntity(horse, target, 1.5f, (int)(_t.CreeperSeconds * 1000));
                    Schedule(_t.CreeperSeconds, "end creep", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Graze:
                    // Trigger 46 checks the grazing scenario plays with a rider on.
                    _game.StartScenario(horse, "WORLD_ANIMAL_HORSE_GRAZING", (int)(_t.GrazerSeconds * 1000), "WORLD_ANIMAL_HORSE_GRAZING");
                    break;
                case BehaviorAction.Unhitch:
                    _game.Unhitch(horse);
                    Schedule(1.0, "wander off", () => _game.WanderNearby(horse, 15f));
                    Schedule(1.0 + _t.WanderSeconds, "end wander", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Roam:
                    _game.WanderFreely(horse);
                    Schedule(_t.WanderlustSeconds, "end roam", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.SnackStop:
                    _game.HorseAction(horse, 3); // quick stop
                    Schedule(1.0, "snack", () => _game.StartScenario(horse, "WORLD_ANIMAL_HORSE_GRAZING", (int)(_t.SnackSeconds * 1000), "WORLD_ANIMAL_HORSE_GRAZING"));
                    break;
                case BehaviorAction.SquareUp:
                    _game.HorseAgitate(horse, false);
                    Vocal(horse, "SNORT");
                    if (target != 0) _game.LookAtEntity(horse, target, 4000);
                    break;
                case BehaviorAction.Fret:
                case BehaviorAction.JealousCall:
                    Vocal(horse, "CALL");
                    _game.HorseAgitate(horse, false);
                    if (target != 0) _game.LookAtEntity(horse, target, 4000);
                    break;
                case BehaviorAction.StareAtNothing:
                {
                    // Trigger 62: a spot about 6 m away, off to one side, where there is nothing to see.
                    Vec3 at = _game.GetPosition(horse), f = _game.GetForward(horse);
                    double side = _random.NextDouble() < 0.5 ? -1 : 1;
                    var spot = new Vec3((float)(at.X + 4 * f.X - side * 4 * f.Y), (float)(at.Y + 4 * f.Y + side * 4 * f.X), at.Z + 1.5f);
                    _game.LookAtPoint(horse, spot, (int)(_t.NightStareSeconds * 1000));
                    break;
                }
                case BehaviorAction.BailOut:
                    _game.HorseAction(horse, 2); // throw rider (Phase 1: works)
                    if (target != 0 && target != player) Schedule(1.2, "bail out", () => _game.FleeFrom(horse, target, 25f, 4000));
                    Schedule(5.5, "end bail out", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.SideStep:
                    // A step or two away (the tested flee task, short and slow).
                    _game.FleeFrom(horse, target, 3f, 1500);
                    Schedule(1.8, "end side-step", () => _game.ClearTasks(horse));
                    break;
                case BehaviorAction.Beg:
                    Vocal(horse, "CONTENT");
                    _game.LookAtEntity(horse, player, 3000);
                    break;
                case BehaviorAction.Ignore:
                    // Trigger 57: after the game starts its come-to-you, cancel it and nose the ground.
                    Schedule(1.5, "ignore whistle", () =>
                    {
                        _game.ClearTasks(horse);
                        _game.StartScenario(horse, "WORLD_ANIMAL_HORSE_GRAZING", (int)(_t.SelectiveHearingSeconds * 1000), "WORLD_ANIMAL_HORSE_GRAZING");
                    });
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

        /// <summary>Every frame: Lazy pace caps the horse's pace while you aren't spurring (the cap lasts one frame).</summary>
        public void EveryFrame(int mount)
        {
            if (_lazyHorse == 0 || mount != _lazyHorse || _lastHold != null) return;
            float ratio = _game.IsSprintPressed ? _unhurriedRatio : _lazyRatio; // Unhurried caps the spur; Lazy pace the rest
            if (ratio > 0) _game.SetMaxMoveBlendRatio(_lazyHorse, ratio);
        }

        /// <summary>A sound from the horse, unless it is a Silent stealth horse and you are sneaking.</summary>
        private void Vocal(int horse, string sound)
        {
            if (_silent.Contains(horse) && _game.IsPlayerSneaking) return;
            _game.PlayVocalization(horse, sound);
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
        private const int MaxNaturalBondGain = 300;

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
            // A jump far bigger than bonding ever gives (the largest natural gain seen in game was +35) was set from outside:
            // DevTools trigger 42 or a trainer's "max bond". Scaling it would undo the jump, so take it as it is.
            if (gained > MaxNaturalBondGain)
            {
                _log.Debug(Log, $"{record.Id}: bond XP jumped +{gained} to {bond.Points} (bond {bond.Rank}), set from outside; not scaled.");
                _bondSeen[horse] = (bond.Rank, bond.Points);
                return;
            }
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
