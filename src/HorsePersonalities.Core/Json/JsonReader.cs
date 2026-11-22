using System.Globalization;
using System.Text;

namespace HorsePersonalities.Core.Json
{
    /// <summary>Strict RFC 8259 JSON parser. Errors carry the line and column so a hand-edited file is easy to fix.</summary>
    internal sealed class JsonReader
    {
        private const int MaxDepth = 64;
        private readonly string _text;
        private int _pos;
        private int _depth;

        private JsonReader(string text) { _text = text; }

        public static JsonValue Parse(string text)
        {
            if (text == null) throw new JsonException("No JSON text.");
            var reader = new JsonReader(text);
            // Tolerate a UTF-8 byte order mark, which Notepad adds.
            if (reader._pos < text.Length && text[reader._pos] == '﻿') reader._pos++;
            reader.SkipWhitespace();
            JsonValue value = reader.ReadValue();
            reader.SkipWhitespace();
            if (reader._pos != text.Length) throw reader.Error("Unexpected text after the end of the JSON value");
            return value;
        }

        private JsonValue ReadValue()
        {
            if (_pos >= _text.Length) throw Error("Unexpected end of JSON");
            char c = _text[_pos];
            switch (c)
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return JsonValue.From(ReadString());
                case 't': ReadLiteral("true"); return JsonValue.From(true);
                case 'f': ReadLiteral("false"); return JsonValue.From(false);
                case 'n': ReadLiteral("null"); return JsonValue.Null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                    throw Error($"Unexpected character '{c}'");
            }
        }

        private JsonValue ReadObject()
        {
            Enter();
            var obj = JsonValue.NewObject();
            _pos++; // {
            SkipWhitespace();
            if (Peek() == '}') { _pos++; _depth--; return obj; }
            while (true)
            {
                SkipWhitespace();
                if (Peek() != '"') throw Error("Expected a quoted key");
                string key = ReadString();
                SkipWhitespace();
                if (Peek() != ':') throw Error("Expected ':' after a key");
                _pos++;
                SkipWhitespace();
                if (obj.Has(key)) throw Error($"Duplicate key \"{key}\"");
                obj.Set(key, ReadValue());
                SkipWhitespace();
                char c = Peek();
                if (c == ',') { _pos++; continue; }
                if (c == '}') { _pos++; break; }
                throw Error("Expected ',' or '}'");
            }
            _depth--;
            return obj;
        }

        private JsonValue ReadArray()
        {
            Enter();
            var arr = JsonValue.NewArray();
            _pos++; // [
            SkipWhitespace();
            if (Peek() == ']') { _pos++; _depth--; return arr; }
            while (true)
            {
                SkipWhitespace();
                arr.Add(ReadValue());
                SkipWhitespace();
                char c = Peek();
                if (c == ',') { _pos++; continue; }
                if (c == ']') { _pos++; break; }
                throw Error("Expected ',' or ']'");
            }
            _depth--;
            return arr;
        }

        private string ReadString()
        {
            _pos++; // opening quote
            var sb = new StringBuilder();
            while (true)
            {
                if (_pos >= _text.Length) throw Error("Unterminated string");
                char c = _text[_pos++];
                if (c == '"') return sb.ToString();
                if (c < 0x20) throw Error("Control character inside a string");
                if (c != '\\') { sb.Append(c); continue; }
                if (_pos >= _text.Length) throw Error("Unterminated escape");
                char e = _text[_pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_pos + 4 > _text.Length) throw Error("Short \\u escape");
                        if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                            throw Error("Bad \\u escape");
                        sb.Append((char)code);
                        _pos += 4;
                        break;
                    default: throw Error($"Unknown escape '\\{e}'");
                }
            }
        }

        private JsonValue ReadNumber()
        {
            int start = _pos;
            if (Peek() == '-') _pos++;
            if (Peek() == '0') _pos++;
            else if (IsDigit(Peek())) while (IsDigit(Peek())) _pos++;
            else throw Error("Bad number");
            if (Peek() == '.')
            {
                _pos++;
                if (!IsDigit(Peek())) throw Error("Bad number: digits must follow '.'");
                while (IsDigit(Peek())) _pos++;
            }
            if (Peek() == 'e' || Peek() == 'E')
            {
                _pos++;
                if (Peek() == '+' || Peek() == '-') _pos++;
                if (!IsDigit(Peek())) throw Error("Bad number exponent");
                while (IsDigit(Peek())) _pos++;
            }
            string s = _text.Substring(start, _pos - start);
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || double.IsInfinity(d))
                throw Error($"Number out of range: {s}");
            return JsonValue.From(d);
        }

        private void ReadLiteral(string literal)
        {
            if (string.CompareOrdinal(_text, _pos, literal, 0, literal.Length) != 0) throw Error($"Expected '{literal}'");
            _pos += literal.Length;
        }

        private void Enter()
        {
            if (++_depth > MaxDepth) throw Error("JSON nested too deeply");
        }

        private char Peek() => _pos < _text.Length ? _text[_pos] : '\0';
        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private void SkipWhitespace()
        {
            while (_pos < _text.Length)
            {
                char c = _text[_pos];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _pos++;
                else break;
            }
        }

        private JsonException Error(string message)
        {
            int line = 1, col = 1;
            for (int i = 0; i < _pos && i < _text.Length; i++)
            {
                if (_text[i] == '\n') { line++; col = 1; }
                else col++;
            }
            return new JsonException($"{message} at line {line}, column {col}.");
        }
    }
}
