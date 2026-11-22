using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Settings
{
    /// <summary>
    /// Minimal INI reader: [Sections], key = value, and ; or # comments. Keys and sections ignore case.
    /// Keys before any section go in the "" section.
    /// </summary>
    public sealed class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public List<string> Warnings { get; } = new List<string>();

        public static IniFile Parse(string text)
        {
            var ini = new IniFile();
            string section = "";
            ini.Section(section);
            if (text == null) return ini;
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[')
                {
                    int end = line.IndexOf(']');
                    if (end < 0) { ini.Warnings.Add($"Line {i + 1}: section header has no ']'."); continue; }
                    section = line.Substring(1, end - 1).Trim();
                    ini.Section(section);
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) { ini.Warnings.Add($"Line {i + 1}: expected key = value."); continue; }
                string key = line.Substring(0, eq).Trim();
                string value = StripInlineComment(line.Substring(eq + 1)).Trim();
                ini.Section(section)[key] = value;
            }
            return ini;
        }

        public bool TryGet(string section, string key, out string value)
        {
            value = null;
            return _sections.TryGetValue(section ?? "", out var s) && s.TryGetValue(key, out value);
        }

        /// <summary>Looks in the section first, then anywhere in the file, so a key in the wrong section still counts.</summary>
        public bool TryGetAnywhere(string section, string key, out string value)
        {
            if (TryGet(section, key, out value)) return true;
            foreach (var s in _sections.Values)
                if (s.TryGetValue(key, out value)) return true;
            return false;
        }

        private Dictionary<string, string> Section(string name)
        {
            if (!_sections.TryGetValue(name, out var s))
            {
                s = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _sections[name] = s;
            }
            return s;
        }

        private static string StripInlineComment(string v)
        {
            // "80 ; percent" -> "80". Only a ; or # preceded by whitespace starts a comment.
            for (int i = 1; i < v.Length; i++)
                if ((v[i] == ';' || v[i] == '#') && char.IsWhiteSpace(v[i - 1])) return v.Substring(0, i);
            return v;
        }
    }
}
