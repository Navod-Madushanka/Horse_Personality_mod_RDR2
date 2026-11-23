using System;
using System.Diagnostics;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Logging;
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

        public HorsePersonalitiesScript()
        {
            try
            {
                _boot = ModBootstrap.Start(BaseDirectory, Log);
                _guard = new FeatureGuard(_boot.Log);
            }
            catch (Exception)
            {
                // Without a log there is nowhere to report this; stay idle rather than crash the game.
                _boot = null;
            }
            Interval = 0;
            Tick += OnTick;
            Aborted += (s, e) => _boot?.Log.Info(Log, "Script stopped (reload or game exit).");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_boot == null) return;
            _tickTimer.Restart();

            // Mount tracking never waits for the startup banner: if a startup native misbehaves, mounts still report.
            if (!_announced) _guard.Run("Startup", Announce);
            _guard.Run("MountTracking", TrackMount);

            _tickTimer.Stop();
            RecordTickTime(_tickTimer.Elapsed.TotalMilliseconds);
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
