using System;
using System.Diagnostics;
using System.Windows.Forms;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;
using HorsePersonalities.Core.Safety;
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
    /// Phase 3: each mounted horse with a breed gets a preview personality roll, shown on the debug overlay
    /// (DebugKey, F9 by default) and written to the log. Not saved yet.
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
        private PreviewRolls _previews;
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
                if (_boot.Breeds != null) _previews = new PreviewRolls(new PersonalityRoller(_boot.Breeds, _boot.Settings));
            }
            catch (Exception)
            {
                // Without a log there is nowhere to report this; stay idle rather than crash the game.
                _boot = null;
            }
            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += (s, e) => _boot?.Log.Info(Log, "Script stopped (reload or game exit).");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_boot == null) return;
            _tickTimer.Restart();

            // Mount tracking never waits for the startup banner: if a startup native misbehaves, mounts still report.
            if (!_announced) _guard.Run("Startup", Announce);
            _guard.Run("MountTracking", TrackMount);
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
                HorseDescription d = HorseDescriber.Describe(_game, _boot.Breeds, change.Current);
                text = d.ToSubtitle();
                _boot.Log.Info(Log, $"Mounted horse {change.Current}: {text} (bond points {d.BondPoints}, model hash 0x{d.ModelHash:X8})");
                Personality p = null;
                bool rolledNow = false;
                _guard.Run("Roller", () => p = _previews?.For(change.Current, d.Breed, out rolledNow));
                if (rolledNow) _boot.Log.Info("Roller", $"Preview roll for horse {change.Current}: {p}");
                _overlay.SetLines(OverlayText.Build(d, p));
            }
            else
            {
                uint model = _game.GetModel(change.Current);
                text = $"Model 0x{model:X8} - breeds.json not loaded";
                _boot.Log.Info(Log, $"Mounted horse {change.Current}: {text}");
            }
            _game.ShowSubtitle(text);
        }

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
