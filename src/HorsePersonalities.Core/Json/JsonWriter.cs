using System.Globalization;
using System.Text;

namespace HorsePersonalities.Core.Json
{
    internal static class JsonWriter
    {
        public static string Write(JsonValue value, bool indented)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, indented, 0);
            if (indented) sb.Append('\n');
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, JsonValue v, bool indented, int depth)
        {
            switch (v.Kind)
            {
                case JsonKind.Null: sb.Append("null"); break;
                case JsonKind.Bool: sb.Append(v.RawBool ? "true" : "false"); break;
                case JsonKind.Number: sb.Append(FormatNumber(v.RawNumber)); break;
                case JsonKind.String: WriteString(sb, v.RawString); break;
                case JsonKind.Array:
                    if (v.Count == 0) { sb.Append("[]"); break; }
                    // Arrays of plain values stay on one line: ["Race", "War"] reads better than one per line.
                    bool flat = true;
                    foreach (var item in v.Items) if (item.Kind == JsonKind.Array || item.Kind == JsonKind.Object) { flat = false; break; }
                    sb.Append('[');
                    for (int i = 0; i < v.Count; i++)
                    {
                        if (i > 0) sb.Append(indented && flat ? ", " : ",");
                        if (indented && !flat) NewLine(sb, depth + 1);
                        WriteValue(sb, v.Items[i], indented, depth + 1);
                    }
                    if (indented && !flat) NewLine(sb, depth);
                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    if (v.Count == 0) { sb.Append("{}"); break; }
                    sb.Append('{');
                    for (int i = 0; i < v.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        if (indented) NewLine(sb, depth + 1);
                        var m = v.Members[i];
                        WriteString(sb, m.Key);
                        sb.Append(indented ? ": " : ":");
                        WriteValue(sb, m.Value, indented, depth + 1);
                    }
                    if (indented) NewLine(sb, depth);
                    sb.Append('}');
                    break;
            }
        }

        /// <summary>Shortest text that reads back to the same double; whole numbers have no decimal point.</summary>
        public static string FormatNumber(double d)
        {
            if (d == System.Math.Floor(d) && System.Math.Abs(d) < 1e15) return ((long)d).ToString(CultureInfo.InvariantCulture);
            string r = d.ToString("R", CultureInfo.InvariantCulture);
            // Prefer a short form like 1.17 over 1.1700000000000002 when it round-trips to within rounding noise.
            for (int digits = 1; digits <= 15; digits++)
            {
                string s = d.ToString("G" + digits, CultureInfo.InvariantCulture);
                if (double.Parse(s, CultureInfo.InvariantCulture) == d) return s;
            }
            return r;
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static void NewLine(StringBuilder sb, int depth)
        {
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }
    }
}
