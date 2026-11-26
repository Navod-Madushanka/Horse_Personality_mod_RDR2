using System;
using System.Globalization;
using System.Text;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Core.Settings
{
    /// <summary>
    /// Player settings from HorsePersonalities.ini (Build plan: Data model). Every value has a default and a valid
    /// range; a missing or bad value falls back to the default with a log line, so a typo never stops the mod.
    /// </summary>
    public sealed class ModSettings
    {
        public const string Section = "HorsePersonalities";
        private const string LogCategory = "Settings";

        public int GoodHorseChance { get; private set; } = 80;
        public int UnbreakableChance { get; private set; } = 5;
        /// <summary>Most ranks a natural stat may move, whatever its tier allows. 0 turns natural stats off.</summary>
        public int MaxStatRankChange { get; private set; } = 2;
        /// <summary>Puts every horse's stats back to the game's own values as the mod sees it, and stops applying natural stats.</summary>
        public bool RestoreVanillaStats { get; private set; } = false;
        public double SpookFrequency { get; private set; } = 1.0;
        public int QuirkCooldownSeconds { get; private set; } = 120;
        public bool DisableDuringMissions { get; private set; } = true;
        public bool DebugOverlay { get; private set; } = false;
        /// <summary>A System.Windows.Forms.Keys name such as F9. The game layer turns it into a key.</summary>
        public string DebugKey { get; private set; } = "F9";
        public bool ResetAllHorses { get; private set; } = false;
        /// <summary>Lowest level written to log.txt: Debug, Info, Warn or Error.</summary>
        public LogLevel LogLevel { get; private set; } = LogLevel.Info;

        public static ModSettings Defaults() => new ModSettings();

        public static ModSettings Load(string iniText, ILog log)
        {
            log = log ?? NullLog.Instance;
            var s = new ModSettings();
            if (iniText == null)
            {
                log.Info(LogCategory, "No HorsePersonalities.ini found; using defaults.");
                return s;
            }
            IniFile ini = IniFile.Parse(iniText);
            foreach (string w in ini.Warnings) log.Warn(LogCategory, "HorsePersonalities.ini " + w);

            s.GoodHorseChance = ReadInt(ini, log, nameof(GoodHorseChance), s.GoodHorseChance, 0, 100);
            s.UnbreakableChance = ReadInt(ini, log, nameof(UnbreakableChance), s.UnbreakableChance, 0, 100);
            s.MaxStatRankChange = ReadInt(ini, log, nameof(MaxStatRankChange), s.MaxStatRankChange, 0, TierCapMaxRank);
            if (ini.TryGetAnywhere(Section, "MaxStatVariance", out _))
                log.Warn(LogCategory, "MaxStatVariance is no longer used: natural stats are whole ranks now. Use MaxStatRankChange (0-9, default 2).");
            s.RestoreVanillaStats = ReadBool(ini, log, nameof(RestoreVanillaStats), s.RestoreVanillaStats);
            if (ini.TryGetAnywhere(Section, "RevealPerBondLevel", out _))
                log.Warn(LogCategory, "RevealPerBondLevel is no longer used: stat differences show at bond 2 and 3 (the sheet's Roller tab).");
            s.SpookFrequency = ReadDouble(ini, log, nameof(SpookFrequency), s.SpookFrequency, 0, 10);
            s.QuirkCooldownSeconds = ReadInt(ini, log, nameof(QuirkCooldownSeconds), s.QuirkCooldownSeconds, 0, 3600);
            s.DisableDuringMissions = ReadBool(ini, log, nameof(DisableDuringMissions), s.DisableDuringMissions);
            s.DebugOverlay = ReadBool(ini, log, nameof(DebugOverlay), s.DebugOverlay);
            s.DebugKey = ReadKeyName(ini, log, nameof(DebugKey), s.DebugKey);
            s.ResetAllHorses = ReadBool(ini, log, nameof(ResetAllHorses), s.ResetAllHorses);
            s.LogLevel = ReadEnum(ini, log, nameof(LogLevel), s.LogLevel);
            return s;
        }

        /// <summary>The file written on first run and shipped in the release zip.</summary>
        public static string DefaultIniText()
        {
            var d = new ModSettings();
            var sb = new StringBuilder();
            sb.AppendLine("; Horse Personalities settings. Delete a line to go back to its default.");
            sb.AppendLine($"[{Section}]");
            sb.AppendLine("; Percent of horses that roll good (0-100).");
            sb.AppendLine($"GoodHorseChance = {d.GoodHorseChance}");
            sb.AppendLine("; Percent cap on unbreakable wild horses (0-100).");
            sb.AppendLine($"UnbreakableChance = {d.UnbreakableChance}");
            sb.AppendLine("; Most ranks a natural stat can move from the breed's usual value (0-9). 0 turns natural stats off.");
            sb.AppendLine($"MaxStatRankChange = {d.MaxStatRankChange}");
            sb.AppendLine("; Puts each horse's stats back to the game's own values as you ride it (true/false). Turn on before uninstalling.");
            sb.AppendLine($"RestoreVanillaStats = {Bool(d.RestoreVanillaStats)}");
            sb.AppendLine("; Multiplier on random spook checks (0-10). 0 turns random spooks off.");
            sb.AppendLine($"SpookFrequency = {d.SpookFrequency.ToString("0.0#", CultureInfo.InvariantCulture)}");
            sb.AppendLine("; Shortest gap between two quirk events on one horse, in seconds (0-3600).");
            sb.AppendLine($"QuirkCooldownSeconds = {d.QuirkCooldownSeconds}");
            sb.AppendLine("; Turns behaviors off in story missions and cutscenes (true/false).");
            sb.AppendLine($"DisableDuringMissions = {Bool(d.DisableDuringMissions)}");
            sb.AppendLine("; Shows hidden values on screen (true/false). For testing.");
            sb.AppendLine($"DebugOverlay = {Bool(d.DebugOverlay)}");
            sb.AppendLine("; Key that toggles the overlay.");
            sb.AppendLine($"DebugKey = {d.DebugKey}");
            sb.AppendLine("; Wipes saved personalities on next load, for a new playthrough (true/false).");
            sb.AppendLine($"ResetAllHorses = {Bool(d.ResetAllHorses)}");
            sb.AppendLine("; How much goes in log.txt: Debug, Info, Warn or Error.");
            sb.AppendLine($"LogLevel = {d.LogLevel}");
            return sb.ToString();
        }

        private const int TierCapMaxRank = Breeds.TierCap.MaxRank;

        private static string Bool(bool b) => b ? "true" : "false";

        private static int ReadInt(IniFile ini, ILog log, string key, int fallback, int min, int max)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            if (!int.TryParse(raw.TrimEnd('%').Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                log.Warn(LogCategory, $"{key} = \"{raw}\" is not a whole number; using {fallback}.");
                return fallback;
            }
            if (v < min || v > max)
            {
                log.Warn(LogCategory, $"{key} = {v} is outside {min}-{max}; using {fallback}.");
                return fallback;
            }
            return v;
        }

        private static double ReadDouble(IniFile ini, ILog log, string key, double fallback, double min, double max)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            // Accept "1,5" as well as "1.5": players in many locales type a decimal comma.
            if (!double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) || double.IsNaN(v))
            {
                log.Warn(LogCategory, $"{key} = \"{raw}\" is not a number; using {fallback.ToString(CultureInfo.InvariantCulture)}.");
                return fallback;
            }
            if (v < min || v > max)
            {
                log.Warn(LogCategory, $"{key} = {v.ToString(CultureInfo.InvariantCulture)} is outside {min}-{max}; using {fallback.ToString(CultureInfo.InvariantCulture)}.");
                return fallback;
            }
            return v;
        }

        private static bool ReadBool(IniFile ini, ILog log, string key, bool fallback)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "true": case "yes": case "on": case "1": return true;
                case "false": case "no": case "off": case "0": return false;
                default:
                    log.Warn(LogCategory, $"{key} = \"{raw}\" is not true or false; using {Bool(fallback)}.");
                    return fallback;
            }
        }

        private static string ReadKeyName(IniFile ini, ILog log, string key, string fallback)
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            string v = raw.Trim();
            if (v.Length == 0 || v.IndexOf(' ') >= 0)
            {
                log.Warn(LogCategory, $"{key} = \"{raw}\" is not a key name; using {fallback}.");
                return fallback;
            }
            return v;
        }

        private static T ReadEnum<T>(IniFile ini, ILog log, string key, T fallback) where T : struct
        {
            if (!ini.TryGetAnywhere(Section, key, out string raw)) return fallback;
            if (Enum.TryParse(raw.Trim(), true, out T v) && Enum.IsDefined(typeof(T), v)) return v;
            log.Warn(LogCategory, $"{key} = \"{raw}\" is not one of {string.Join(", ", Enum.GetNames(typeof(T)))}; using {fallback}.");
            return fallback;
        }
    }
}
