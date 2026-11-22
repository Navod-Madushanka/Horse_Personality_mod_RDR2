using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Settings;

namespace HorsePersonalities.DevTools
{
    /// <summary>
    /// Keys and test lists for the trigger spike, from scripts/HorsePersonalities.DevTools.ini (written on first run).
    /// Change keys here if one clashes with your game bindings; add sound names to try without rebuilding.
    /// </summary>
    public sealed class DevToolsConfig
    {
        private const string Section = "DevTools";

        public Keys PanelKey = Keys.F10;
        public Keys UpKey = Keys.PageUp;
        public Keys DownKey = Keys.PageDown;
        public Keys FireKey = Keys.End;
        public Keys WorksKey = Keys.Home;
        public Keys FailsKey = Keys.Delete;

        /// <summary>Names passed to PLAY_ANIMAL_VOCALIZATION. Unverified guesses: Phase 1 finds which ones make a sound.</summary>
        public List<string> Vocalizations = new List<string> { "SNORT", "WHINNY", "NEIGH", "HORSE_SNORT", "HORSE_WHINNY", "AGITATED", "CONTENT", "CALL" };

        /// <summary>Scenario names tried for grazing and resting. From femga's scenario list.</summary>
        public List<string> Scenarios = new List<string> { "WORLD_ANIMAL_HORSE_GRAZING", "WORLD_ANIMAL_HORSE_GRAZING_DOMESTIC", "WORLD_ANIMAL_HORSE_RESTING_DOMESTIC", "WORLD_ANIMAL_HORSE_DRINK_GROUND_DOMESTIC", "WORLD_ANIMAL_HORSE_WALLOW" };

        public static string DefaultText() =>
@"; Horse Personalities DevTools (Phase 1 trigger spike). Not part of the released mod.
; Key names are System.Windows.Forms.Keys names: F10, PageUp, NumPad5, Oemcomma, ...
[DevTools]
PanelKey = F10
UpKey = PageUp
DownKey = PageDown
FireKey = End
WorksKey = Home
FailsKey = Delete
; Comma-separated. Each becomes one ""Sound"" trigger in the list. Add more names to try them.
Vocalizations = SNORT, WHINNY, NEIGH, HORSE_SNORT, HORSE_WHINNY, AGITATED, CONTENT, CALL
Scenarios = WORLD_ANIMAL_HORSE_GRAZING, WORLD_ANIMAL_HORSE_GRAZING_DOMESTIC, WORLD_ANIMAL_HORSE_RESTING_DOMESTIC, WORLD_ANIMAL_HORSE_DRINK_GROUND_DOMESTIC, WORLD_ANIMAL_HORSE_WALLOW
";

        public static DevToolsConfig Load(string path, ILog log)
        {
            var c = new DevToolsConfig();
            string text = null;
            try
            {
                if (File.Exists(path)) text = File.ReadAllText(path);
                else File.WriteAllText(path, DefaultText());
            }
            catch (Exception ex)
            {
                log.Error("DevTools", "Could not read or write " + path, ex);
            }
            if (text == null) return c;

            IniFile ini = IniFile.Parse(text);
            c.PanelKey = ReadKey(ini, log, "PanelKey", c.PanelKey);
            c.UpKey = ReadKey(ini, log, "UpKey", c.UpKey);
            c.DownKey = ReadKey(ini, log, "DownKey", c.DownKey);
            c.FireKey = ReadKey(ini, log, "FireKey", c.FireKey);
            c.WorksKey = ReadKey(ini, log, "WorksKey", c.WorksKey);
            c.FailsKey = ReadKey(ini, log, "FailsKey", c.FailsKey);
            c.Vocalizations = ReadList(ini, "Vocalizations", c.Vocalizations);
            c.Scenarios = ReadList(ini, "Scenarios", c.Scenarios);
            return c;
        }

        private static Keys ReadKey(IniFile ini, ILog log, string key, Keys fallback)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            if (Enum.TryParse(raw.Trim(), true, out Keys k)) return k;
            log.Warn("DevTools", $"{key} = \"{raw}\" is not a key name; using {fallback}.");
            return fallback;
        }

        private static List<string> ReadList(IniFile ini, string key, List<string> fallback)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            var list = new List<string>();
            foreach (string s in raw.Split(','))
                if (s.Trim().Length > 0) list.Add(s.Trim());
            return list.Count > 0 ? list : fallback;
        }
    }
}
