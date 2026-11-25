using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Safety;
using HorsePersonalities.Core.Saving;
using HorsePersonalities.Core.Settings;
using HorsePersonalities.Core.Stats;
using HorsePersonalities.Core.Testing;
using HorsePersonalities.Core.Timing;
using HorsePersonalities.Core.Tracking;
using RDR2;
using PedAttribute = HorsePersonalities.Core.GameApi.PedAttribute;

namespace HorsePersonalities.Game
{
    /// <summary>
    /// The mod's entry point. ScriptHookRDR2DotNet finds this class because it derives from Script and has a public
    /// parameterless constructor.
    ///
    /// Phase 0: shows "Horse Personalities loaded" once the game is playable, and on every mount shows the horse's
    /// breed, model name, sex and bond level. Nothing changes in the game yet.
    /// Phase 2: a mounted horse gets a temporary personality; the player's saddle horse gets a permanent one,
    /// kept by fingerprint (model, sex, coat).
    /// Phase 3: personalities come from the roller; the debug overlay (DebugKey, F9 by default) shows them.
    /// Phase 4: permanent personalities load from horses.json at start and are written when one is added and when
    /// the script stops.
    /// Phase 5: every second, the horse you ride and your saddle horse get their health and stamina base ranks set to
    /// their original + the natural difference revealed so far by bond level (bond 2 and 3).
    /// Phase 6: spooks, bucks, refusals and kicks from the horse's traits (BehaviorRunner, every 250 ms), bond XP scaled
    /// by Bond affinity and quality (every second), Hard and Unbreakable breaking, all held during missions and cutscenes.
    /// </summary>
    public sealed class HorsePersonalitiesScript : Script
    {
        private const string Log = "Main";

        private readonly RdrGameApi _game = new RdrGameApi();
        private readonly Stopwatch _sinceStart = Stopwatch.StartNew();
        private long _nextDiagMs = 3000;
        private readonly MountWatcher _mounts = new MountWatcher();
        private readonly IntervalGate _perfReport = new IntervalGate(60_000);
        private readonly Stopwatch _tickTimer = new Stopwatch();
        private ModBootstrap _boot;
        private FeatureGuard _guard;
        private bool _announced;
        private double _worstTickMs;
        private double _totalTickMs;
        private int _ticks;
        private readonly DebugOverlay _overlay = new DebugOverlay();
        private HorseRegistry _registry;
        private HorseSaveStore _store;
        private readonly IntervalGate _saddlePoll = new IntervalGate(250);
        private readonly IntervalGate _statCheck = new IntervalGate(1000);
        private readonly IntervalGate _behaviorCheck = new IntervalGate(250);
        private BehaviorRunner _behavior;
        private PersonalityRoller _roller;
        private TestPresets _presets;
        private Keys _presetKey = Keys.F11;
        private readonly HashSet<int> _wildMounted = new HashSet<int>();
        private int _saddleHorse = -1;
        private Keys _debugKey = Keys.F9;
        private double _lastTickMs;

        public HorsePersonalitiesScript()
        {
            try
            {
                _boot = ModBootstrap.Start(BaseDirectory, Log);
                _guard = new FeatureGuard(_boot.Log);
                _overlay.Visible = _boot.Settings.DebugOverlay;
                if (!Enum.TryParse(_boot.Settings.DebugKey, true, out _debugKey))
                {
                    _boot.Log.Warn("Settings", $"DebugKey = \"{_boot.Settings.DebugKey}\" is not a key name; using F9.");
                    _debugKey = Keys.F9;
                }
                if (_boot.Breeds != null) StartRegistry();
            }
            catch (Exception)
            {
                // Without a log there is nowhere to report this; stay idle rather than crash the game.
                _boot = null;
            }
            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += (s, e) =>
            {
                if (_boot == null) return;
                SaveIfChanged("script stopping");
                _boot.Log.Info(Log, "Script stopped (reload or game exit).");
            };
        }

        /// <summary>Loads saved horses (or resets them for a new playthrough). Runs once, at script start.</summary>
        private void StartRegistry()
        {
            var roller = _roller = new PersonalityRoller(_boot.Breeds, _boot.Settings);
            _registry = new HorseRegistry(roller, _boot.Log, isPrizedModel: id => _boot.Breeds.TryGetPrized(id.Model, out _));
            bool test = _boot.Settings.TestPersonalities && LoadPresets();
            if (test)
            {
                // Test mode (Build plan: Test mode): no random rolls; each new horse gets its model's preset, or Average.
                _registry.UseInsteadOfRoller((id, breed, origin, seed) =>
                {
                    TestPreset preset = _presets.ForModel(id.Model);
                    return (preset.For(breed, origin, seed, id.Model, roller, _boot.Breeds), preset.Name);
                });
                if (!Enum.TryParse(_boot.Settings.TestPresetKey, true, out _presetKey)) _presetKey = Keys.F11;
                _boot.Log.Info("TestMode", $"Test mode on: presets instead of rolls, saved to horses.test.json. {_presetKey} gives your horse the next preset.");
            }
            // A schema 1 save (natural stats as fractions) gets its stats re-rolled from each horse's seed, as whole ranks.
            _store = new HorseSaveStore(_boot.Paths.DataDir, _boot.Log, (breed, origin, seed) =>
                _boot.Breeds.TryGetBreed(breed, out Breed b) ? roller.Roll(b, origin, seed, origin == HorseOrigin.Stable).NaturalStats : new NaturalStats(0, 0, 0, 0),
                test ? "horses.test" : "horses");
            SaveLoadResult loaded = _store.Load(_boot.Settings.ResetAllHorses, DateTime.Now);
            _registry.Load(loaded.Records);
            // Saves only happen on events, and the hook never tells the script when the game exits (no "Script stopped" line ever
            // reached the log), so write straight away after an upgrade or a recovery from the backup: otherwise horses.json would
            // stay missing until some later event.
            if (loaded.Upgraded && _store.Save(_registry.Permanent))
                _boot.Log.Info("Save", "Wrote the upgraded horses.json.");
            else if (loaded.Source == SaveSource.Backup && _store.Save(_registry.Permanent))
                _boot.Log.Info("Save", "Wrote horses.json again after loading the backup.");
            _behavior = new BehaviorRunner(_game, _boot.Breeds, _boot.Settings, _registry, _boot.Log);
            if (_boot.Settings.ResetAllHorses) TurnOffReset();
        }

        private bool LoadPresets()
        {
            string path = Path.Combine(_boot.Paths.DataDir, "test-personalities.json");
            try
            {
                _presets = TestPresets.Load(File.ReadAllText(path), _boot.Log);
                return true;
            }
            catch (Exception ex)
            {
                _boot.Log.Error("TestMode", $"TestPersonalities is on, but {path} could not be used; rolling normally", ex);
                _game.ShowSubtitle("Horse Personalities: test-personalities.json is missing or broken; see log.txt");
                return false;
            }
        }

        /// <summary>Test mode: the horse you ride (or your last horse within 15 m) gets the next preset.</summary>
        private void NextPreset()
        {
            int player = _game.PlayerPed;
            int horse = _mounts.CurrentMount != 0 ? _mounts.CurrentMount : _game.GetLastMount(player);
            if (horse == 0 || !_game.Exists(horse) || (_mounts.CurrentMount == 0 && _game.DistanceBetween(horse, player) > 15f))
            {
                _game.ShowSubtitle("Test mode: ride a horse (or stand next to it) first.");
                return;
            }
            HorseDescription d = HorseDescriber.Describe(_game, _boot.Breeds, horse);
            if (d.Breed == null) { _game.ShowSubtitle("Test mode: this horse has no breed, so it stays vanilla."); return; }
            HorseFingerprint id = Fingerprint(d);
            HorseRecord current = _registry.Find(horse, id)
                ?? _registry.OnMount(horse, id, d.Breed, HorseOrigin.Unknown, horse == _game.GetSaddleHorse());
            TestPreset next = _presets.Next(current?.Preset);
            HorseRecord replaced = _registry.Replace(horse, id, next.For(d.Breed, current.Personality.Origin, current.Personality.Seed, d.ModelName, _roller, _boot.Breeds), next.Name);
            _overlay.SetLines(OverlayText.Build(d, replaced));
            _game.ShowSubtitle($"Test preset: {next.Name}. {next.Purpose}");
            SaveIfChanged("test preset changed");
        }

        /// <summary>ResetAllHorses wipes once, not on every load: put the INI line back to false.</summary>
        private void TurnOffReset()
        {
            try
            {
                string path = _boot.Paths.SettingsFile;
                File.WriteAllText(path, IniEdit.SetValue(File.ReadAllText(path), nameof(ModSettings.ResetAllHorses), "false"));
                _boot.Log.Info("Settings", "Set ResetAllHorses back to false.");
            }
            catch (Exception ex)
            {
                _boot.Log.Error("Settings", "Could not set ResetAllHorses back to false; it will reset again next load", ex);
            }
        }

        /// <summary>Writes horses.json only when a permanent personality changed (rule 7: events only).</summary>
        private void SaveIfChanged(string why)
        {
            if (_registry == null || !_registry.Dirty) return;
            if (_store.Save(_registry.Permanent))
            {
                _registry.MarkSaved();
                _boot.Log.Info("Save", $"Wrote horses.json ({_registry.Permanent.Count} horses): {why}.");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_boot == null) return;
            _tickTimer.Restart();

            // Mount tracking never waits for the startup banner: if a startup native misbehaves, mounts still report.
            if (!_announced) _guard.Run("Startup", Announce);
            _guard.Run("Events", ReadEvents);
            _guard.Run("MountTracking", TrackMount);
            if (_registry != null && _saddlePoll.Due(Environment.TickCount)) _guard.Run("SaddleWatch", WatchSaddle);
            if (_registry != null && _statCheck.Due(Environment.TickCount))
            {
                _guard.Run("Stats", ApplyStats);
                _guard.Run("Bonding", () => _behavior.UpdateBonding(_mounts.CurrentMount));
            }
            if (_behavior != null && _behaviorCheck.Due(Environment.TickCount))
                _guard.Run("Behaviors", () => _behavior.Update(_mounts.CurrentMount, _game.GameTimeMs / 1000.0));
            _guard.Run("Overlay", () => _overlay.Draw(_game, _lastTickMs));

            _tickTimer.Stop();
            _lastTickMs = _tickTimer.Elapsed.TotalMilliseconds;
            RecordTickTime(_lastTickMs);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_boot == null) return;
            if (_presets != null && e.KeyCode == _presetKey)
            {
                _guard.Run("TestMode", NextPreset);
                return;
            }
            if (e.KeyCode != _debugKey) return;
            _overlay.Visible = !_overlay.Visible;
            _boot.Log.Debug(Log, "Debug overlay " + (_overlay.Visible ? "on" : "off"));
        }

        private void Announce()
        {
            // In game, GET_IS_LOADING_SCREEN_ACTIVE (and the hook's IsLoading) and IS_SCREEN_FADED_OUT all read true for
            // the whole session, so none of them is used. IS_PLAYER_PLAYING is correct from the first second; give the
            // world a few seconds after the script starts so the banner is not shown over a loading screen.
            bool ready = _game.IsPlayerPlaying && _sinceStart.ElapsedMilliseconds > 5_000;
            // Safety net: never leave the banner waiting forever. Log what the checks said so a bad native shows up.
            if (!ready && _sinceStart.ElapsedMilliseconds > 30_000) ready = true;
            if (!ready)
            {
                if (_sinceStart.ElapsedMilliseconds >= _nextDiagMs)
                {
                    _nextDiagMs += 5000;
                    _boot.Log.Info(Log, "Waiting to announce: " + _game.StartupDiagnostics());
                }
                return;
            }
            _boot.Log.Info(Log, "Startup check passed: " + _game.StartupDiagnostics());
            _announced = true;
            string message = "Horse Personalities loaded";
            if (_boot.BreedsError != null) message += " - " + _boot.BreedsError;
            else message += $" ({_boot.Breeds.Breeds.Count} breeds)";
            _game.ShowSubtitle(message);
            _boot.Log.Info(Log, message);
        }

        private void TrackMount()
        {
            MountChange change = _mounts.Update(_game);
            if (change.Kind != MountChangeKind.Mounted && change.Kind != MountChangeKind.Switched) return;

            string text;
            if (_boot.Breeds != null)
            {
                int horse = change.Current;
                HorseDescription d = HorseDescriber.Describe(_game, _boot.Breeds, horse);
                text = d.ToSubtitle();
                bool saddle = horse == _game.GetSaddleHorse();
                HorseOrigin origin = _wildMounted.Contains(horse) ? HorseOrigin.Wild : HorseOrigin.Unknown;
                // Taming state is logged so Phase 2 can check whether it tells wild horses apart too.
                _boot.Log.Info(Log, $"Mounted horse {horse}: {text} (bond points {d.BondPoints}, model hash 0x{d.ModelHash:X8}, saddle horse {saddle}, origin {origin}, taming state {_game.GetTamingState(horse)})");
                HorseRecord record = null;
                _guard.Run("Registry", () => record = _registry?.OnMount(horse, Fingerprint(d), d.Breed, origin, saddle));
                _overlay.SetLines(OverlayText.Build(d, record));
                SaveIfChanged("mounted the saddle horse");
            }
            else
            {
                uint model = _game.GetModel(change.Current);
                text = $"Model 0x{model:X8} - breeds.json not loaded";
                _boot.Log.Info(Log, $"Mounted horse {change.Current}: {text}");
            }
            _game.ShowSubtitle(text);
        }

        /// <summary>EVENT_PLAYER_MOUNT_WILD_HORSE marks a horse as wild for the session, so its roll includes breaking.</summary>
        private void ReadEvents()
        {
            IReadOnlyList<GameEvent> events = _game.ReadAiEvents();
            _behavior?.OnEvents(events);
            _behavior?.EveryFrame(_mounts.CurrentMount);
            foreach (GameEvent e in events)
                if (e.Hash == GameEvents.PlayerMountWildHorse.Hash && e.Data.Length >= 1 && _wildMounted.Add((int)e.Data[0]))
                    _boot.Log.Info(Log, $"Horse {e.Data[0]} is wild (EVENT_PLAYER_MOUNT_WILD_HORSE).");
        }

        /// <summary>Every 250 ms: a new saddle horse (saddle swap, purchase, or the starting horse on load) becomes permanent.</summary>
        private void WatchSaddle()
        {
            int saddle = _game.GetSaddleHorse();
            if (saddle == _saddleHorse) return;
            _saddleHorse = saddle;
            if (saddle == 0 || !_game.Exists(saddle)) return;
            HorseDescription d = HorseDescriber.Describe(_game, _boot.Breeds, saddle);
            HorseRecord record = _registry.OnSaddle(saddle, Fingerprint(d), d.Breed);
            _boot.Log.Info(Log, $"Saddle horse is now {saddle} ({d.ModelName}): " + (record == null ? "no breed, stays vanilla." : $"permanent personality {record.Id}."));
            if (record != null && _mounts.CurrentMount == saddle) _overlay.SetLines(OverlayText.Build(d, record));
            SaveIfChanged("saddle horse changed");
        }

        /// <summary>
        /// Every second: health and stamina base ranks for the horse you ride and your saddle horse (Phase 5).
        /// Writes only when a rank differs from its target, so a game reset (load, stable) is put right within a second.
        /// </summary>
        private void ApplyStats()
        {
            int mount = _mounts.CurrentMount, saddle = _saddleHorse > 0 ? _saddleHorse : 0;
            foreach (int horse in mount == saddle ? new[] { mount } : new[] { mount, saddle })
            {
                if (horse == 0 || !_game.Exists(horse)) continue;
                HorseDescription d = HorseDescriber.Describe(_game, _boot.Breeds, horse);
                if (d.Breed == null) continue;
                HorseRecord record = _registry.Find(horse, Fingerprint(d));
                if (record == null || record.Status != PersonalityStatus.Permanent) continue;

                RollerTuning t = _boot.Breeds.Roller;
                List<StatChange> changes = StatPlanner.Plan(record, d.BondRank,
                    _game.ReadAttribute(horse, PedAttribute.Health), _game.ReadAttribute(horse, PedAttribute.Stamina),
                    t.RevealFirstRankBond, t.RevealSecondRankBond, _boot.Settings.RestoreVanillaStats, out var learned);
                foreach (var (stat, baseRank) in learned)
                {
                    _registry.RememberVanillaBase(record, stat, baseRank);
                    _boot.Log.Info("Stats", $"{record.Id}: original {stat} base rank is {baseRank}.");
                }
                foreach (StatChange c in changes)
                {
                    _game.SetAttributeBaseRank(horse, c.Attribute, c.To);
                    _boot.Log.Info("Stats", $"{record.Id} at bond {d.BondRank}: {c}{(_boot.Settings.RestoreVanillaStats ? " [RestoreVanillaStats]" : "")}");
                }
            }
            SaveIfChanged("stored original stat ranks");
        }

        private static HorseFingerprint Fingerprint(HorseDescription d) => new HorseFingerprint(d.ModelName, d.IsMale);

        private void RecordTickTime(double ms)
        {
            _ticks++;
            _totalTickMs += ms;
            if (ms > _worstTickMs) _worstTickMs = ms;
            if (!_perfReport.Due(Environment.TickCount)) return;
            if (_ticks > 1)
            {
                double average = _totalTickMs / _ticks;
                // Over budget shows in a normal (Info) log; within budget only at the Debug level.
                LogLevel level = average > 1.0 ? LogLevel.Warn : LogLevel.Debug;
                _boot.Log.Write(level, "Perf", $"Tick time over the last minute: average {average:0.000} ms, worst {_worstTickMs:0.000} ms over {_ticks} ticks (budget 1 ms).");
            }
            _ticks = 0;
            _totalTickMs = 0;
            _worstTickMs = 0;
        }
    }
}
