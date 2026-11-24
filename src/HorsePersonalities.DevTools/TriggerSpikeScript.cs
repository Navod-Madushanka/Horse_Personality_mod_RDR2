using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using HorsePersonalities.Core.GameApi;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Safety;
using HorsePersonalities.Game;
using RDR2;

namespace HorsePersonalities.DevTools
{
    /// <summary>
    /// Phase 1 trigger spike. F10 opens a list of every behavior the build plan wants to use. Select one with
    /// PageUp/PageDown, fire it at your horse with End, then press Home if it worked or Delete if it didn't.
    /// Results go to scripts/HorsePersonalities/phase1-results.csv for the sheet's Quirks tab.
    /// Detectors (pat, brush, feed, saddle swap, mission) run the whole time and print what they see.
    /// Not part of the released mod.
    /// </summary>
    public sealed class TriggerSpikeScript : Script, ITriggerContext
    {
        private const string Log = "DevTools";
        private const int VisibleRows = 9;

        private readonly RdrGameApi _game = new RdrGameApi();
        private bool _loggedDeadReading;
        private readonly List<(int Due, Action Action)> _scheduled = new List<(int, Action)>();
        private readonly List<string> _feed = new List<string>();
        private readonly Dictionary<string, string> _results = new Dictionary<string, string>();
        private ModBootstrap _boot;
        private FeatureGuard _guard;
        private DevToolsConfig _config;
        private List<Trigger> _triggers;
        private Detectors _detectors;
        private string _resultsFile;
        private bool _open;
        private int _selected;
        private string _lastStatus = "";
        private int _horse;
        private int _now;

        public TriggerSpikeScript()
        {
            try
            {
                _boot = ModBootstrap.Start(BaseDirectory, Log);
                _guard = new FeatureGuard(_boot.Log);
                _config = DevToolsConfig.Load(Path.Combine(BaseDirectory, "HorsePersonalities.DevTools.ini"), _boot.Log);
                _triggers = TriggerList.Build(_config);
                _detectors = new Detectors(Report);
                _resultsFile = Path.Combine(_boot.Paths.DataDir, "phase1-results.csv");
                _boot.Log.Info(Log, $"Trigger spike ready: {_triggers.Count} triggers. {_config.PanelKey} opens the panel.");
            }
            catch (Exception)
            {
                _boot = null;
            }
            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        // ---- ITriggerContext ----
        public IGameApi Game => _game;
        public int Horse => _horse;
        public int Now => _now;
        public void After(int ms, Action action) => _scheduled.Add((unchecked(_now + ms), action));
        void ITriggerContext.Log(string message) => Report(message);

        private void OnTick(object sender, EventArgs e)
        {
            if (_boot == null) return;
            _guard.Run("DevTools.Frame", () =>
            {
                _now = _game.GameTimeMs;
                _horse = FindHorse();
                RunScheduled();
            });
            _guard.Run("DevTools.Detectors", () => _detectors.Update(_game, _horse, _now));
            if (_open) _guard.Run("DevTools.Panel", DrawPanel);
        }

        private int FindHorse()
        {
            int player = _game.PlayerPed;
            foreach (int candidate in new[] { _game.GetMount(player), _game.GetLastMount(player), _game.GetSaddleHorse() })
            {
                if (candidate == 0 || !_game.Exists(candidate)) continue;
                if (!_game.IsDead(candidate)) return candidate;
                if (!_loggedDeadReading)
                {
                    _loggedDeadReading = true;
                    _boot.Log.Info(Log, $"Horse {candidate} exists but IS_ENTITY_DEAD says dead (raw 0x{_game.RawResult(Natives.IS_ENTITY_DEAD, candidate):X}); skipping it.");
                }
            }
            return 0;
        }

        private void RunScheduled()
        {
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (unchecked(_now - _scheduled[i].Due) < 0) continue;
                Action a = _scheduled[i].Action;
                _scheduled.RemoveAt(i);
                _guard.Run("DevTools.Scheduled", a);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_boot == null) return;
            _guard.Run("DevTools.Keys", () =>
            {
                if (e.KeyCode == _config.PanelKey) { _open = !_open; return; }
                if (!_open) return;
                if (e.KeyCode == _config.UpKey) _selected = (_selected - 1 + _triggers.Count) % _triggers.Count;
                else if (e.KeyCode == _config.DownKey) _selected = (_selected + 1) % _triggers.Count;
                else if (e.KeyCode == _config.FireKey) Fire(_triggers[_selected]);
                else if (e.KeyCode == _config.WorksKey) Record(_triggers[_selected], "Works");
                else if (e.KeyCode == _config.FailsKey) Record(_triggers[_selected], "Doesn't work");
            });
        }

        private void Fire(Trigger t)
        {
            if (t.NeedsHorse && _horse == 0)
            {
                _lastStatus = "No horse found: ride one, or stand near your last horse.";
                return;
            }
            try
            {
                _lastStatus = t.Run(this);
                _boot.Log.Info(Log, $"Fired {t.Id} on horse {_horse} ({Describe(_horse)}): {_lastStatus}");
            }
            catch (Exception ex)
            {
                // A native that throws is a result too: report it and keep going.
                _lastStatus = $"Threw {ex.GetType().Name}: {ex.Message}";
                _boot.Log.Error(Log, $"Trigger {t.Id} threw", ex);
            }
        }

        private void Record(Trigger t, string result)
        {
            _results[t.Id] = result;
            try
            {
                bool newFile = !File.Exists(_resultsFile);
                using (var w = new StreamWriter(_resultsFile, append: true))
                {
                    if (newFile) w.WriteLine("time,trigger_id,trigger,used_for,result,horse_model,last_status");
                    w.WriteLine(string.Join(",", new[]
                    {
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        t.Id, Csv(t.Name), Csv(t.UsedFor), result, Csv(Describe(_horse)), Csv(_lastStatus),
                    }));
                }
                _lastStatus = $"Recorded \"{result}\" for {t.Name}";
            }
            catch (Exception ex)
            {
                _lastStatus = "Could not write phase1-results.csv: " + ex.Message;
            }
            _boot.Log.Info(Log, $"Result {t.Id}: {result}");
        }

        private string Describe(int horse)
        {
            if (horse == 0) return "none";
            uint model = _game.GetModel(horse);
            return _boot.Breeds != null ? _boot.Breeds.DescribeModel(model) : $"0x{model:X8}";
        }

        private void Report(string message)
        {
            _feed.Add($"{DateTime.Now:HH:mm:ss} {message}");
            if (_feed.Count > 6) _feed.RemoveAt(0);
            _boot.Log.Info("Detector", message);
        }

        private void DrawPanel()
        {
            float x = 20, y = 20, line = 18, scale = 0.3f;
            _game.DrawText($"HORSE PERSONALITIES - PHASE 1 TRIGGERS   horse: {Describe(_horse)}", x, y, 0.35f);
            y += line + 4;
            _game.DrawText($"{_config.UpKey}/{_config.DownKey} select   {_config.FireKey} fire   {_config.WorksKey} works   {_config.FailsKey} doesn't work   {_config.PanelKey} close", x, y, scale);
            y += line + 6;

            int first = Math.Max(0, Math.Min(_selected - VisibleRows / 2, _triggers.Count - VisibleRows));
            for (int i = first; i < Math.Min(first + VisibleRows, _triggers.Count); i++)
            {
                Trigger t = _triggers[i];
                string mark = _results.TryGetValue(t.Id, out string r) ? (r == "Works" ? "[OK] " : "[X]  ") : "[ ]  ";
                _game.DrawText($"{(i == _selected ? "> " : "  ")}{mark}{i + 1:00}. {t.Name}", x, y, scale);
                y += line;
            }

            Trigger sel = _triggers[_selected];
            y += 6;
            _game.DrawText("For: " + sel.UsedFor, x, y, scale); y += line;
            _game.DrawText("How: " + sel.HowToTest, x, y, scale); y += line;
            _game.DrawText("Last: " + _lastStatus, x, y, scale); y += line + 6;
            _game.DrawText("Detectors:", x, y, scale); y += line;
            foreach (string f in _feed) { _game.DrawText("  " + f, x, y, scale); y += line; }
        }

        private static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
    }
}
