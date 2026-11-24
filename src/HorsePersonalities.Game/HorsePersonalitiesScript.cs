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
using HorsePersonalities.Core.Timing;
using HorsePersonalities.Core.Tracking;
using RDR2;

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
            _registry = new HorseRegistry(new PersonalityRoller(_boot.Breeds, _boot.Settings), _boot.Log);
            _store = new HorseSaveStore(_boot.Paths.DataDir, _boot.Log);
            SaveLoadResult loaded = _store.Load(_boot.Settings.ResetAllHorses, DateTime.Now);
            _registry.Load(loaded.Records);
            if (_boot.Settings.ResetAllHorses) TurnOffReset();
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
            _guard.Run("Overlay", () => _overlay.Draw(_game, _lastTickMs));

            _tickTimer.Stop();
            _lastTickMs = _tickTimer.Elapsed.TotalMilliseconds;
            RecordTickTime(_lastTickMs);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_boot == null || e.KeyCode != _debugKey) return;
            _overlay.Visible = !_overlay.Visible;
            _boot.Log.Debug(Log, "Debug overlay " + (_overlay.Visible ? "on" : "off"));
        }

        private void Announce()
        {
            // GET_IS_LOADING_SCREEN_ACTIVE reads true for the whole session in game (the hook's IsLoading too), so it is
            // not used. A playing player who is not faded out means the world is up.
            bool ready = _game.IsPlayerPlaying && !_game.IsScreenFadedOut;
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
            foreach (GameEvent e in _game.ReadAiEvents())
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

        private static HorseFingerprint Fingerprint(HorseDescription d) => new HorseFingerprint(d.ModelName, d.IsMale);

        private void RecordTickTime(double ms)
        {
            _ticks++;
            _totalTickMs += ms;
            if (ms > _worstTickMs) _worstTickMs = ms;
            if (!_perfReport.Due(Environment.TickCount)) return;
            if (_ticks > 1)
                _boot.Log.Debug("Perf", $"Tick time over the last minute: average {_totalTickMs / _ticks:0.000} ms, worst {_worstTickMs:0.000} ms over {_ticks} ticks (budget 1 ms).");
            _ticks = 0;
            _totalTickMs = 0;
            _worstTickMs = 0;
        }
    }
}
