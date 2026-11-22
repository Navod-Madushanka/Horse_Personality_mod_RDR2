using System;
using System.Collections.Generic;
using System.Globalization;

namespace HorsePersonalities.Core.Json
{
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    /// <summary>
    /// A small JSON tree. The mod ships as one DLL inside the game's scripts folder, so it carries its
    /// own reader and writer instead of depending on System.Text.Json or Newtonsoft and their extra DLLs.
    /// Object keys keep their insertion order so written files diff cleanly.
    /// </summary>
    public sealed class JsonValue
    {
        public static readonly JsonValue Null = new JsonValue(JsonKind.Null);

        private readonly bool _bool;
        private readonly double _number;
        private readonly string _string;
        private readonly List<JsonValue> _items;
        private readonly List<KeyValuePair<string, JsonValue>> _members;
        private readonly Dictionary<string, int> _index;

        public JsonKind Kind { get; }

        private JsonValue(JsonKind kind)
        {
            Kind = kind;
            if (kind == JsonKind.Array) _items = new List<JsonValue>();
            if (kind == JsonKind.Object)
            {
                _members = new List<KeyValuePair<string, JsonValue>>();
                _index = new Dictionary<string, int>(StringComparer.Ordinal);
            }
        }

        private JsonValue(bool value) : this(JsonKind.Bool) { _bool = value; }
        private JsonValue(double value) : this(JsonKind.Number) { _number = value; }
        private JsonValue(string value) : this(JsonKind.String) { _string = value ?? throw new ArgumentNullException(nameof(value)); }

        public static JsonValue From(bool value) => new JsonValue(value);
        public static JsonValue From(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("JSON cannot hold NaN or infinity.", nameof(value));
            return new JsonValue(value);
        }
        public static JsonValue From(string value) => value == null ? Null : new JsonValue(value);
        public static JsonValue NewArray() => new JsonValue(JsonKind.Array);
        public static JsonValue NewObject() => new JsonValue(JsonKind.Object);

        public static JsonValue Parse(string text) => JsonReader.Parse(text);
        public string ToJson(bool indented = true) => JsonWriter.Write(this, indented);
        public override string ToString() => ToJson(false);

        // ---- Reading ----

        public bool IsNull => Kind == JsonKind.Null;

        public bool AsBool()
        {
            Expect(JsonKind.Bool);
            return _bool;
        }

        public double AsNumber()
        {
            Expect(JsonKind.Number);
            return _number;
        }

        public int AsInt()
        {
            double n = AsNumber();
            if (n != Math.Floor(n) || n < int.MinValue || n > int.MaxValue)
                throw new JsonException($"Expected a whole number, got {n.ToString(CultureInfo.InvariantCulture)}.");
            return (int)n;
        }

        public string AsString()
        {
            Expect(JsonKind.String);
            return _string;
        }

        public IReadOnlyList<JsonValue> Items
        {
            get { Expect(JsonKind.Array); return _items; }
        }

        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members
        {
            get { Expect(JsonKind.Object); return _members; }
        }

        public int Count => Kind == JsonKind.Array ? _items.Count : Kind == JsonKind.Object ? _members.Count : 0;

        public bool Has(string key) => Kind == JsonKind.Object && _index.ContainsKey(key);

        /// <summary>Member by key, or <see cref="Null"/> if this is not an object or the key is missing.</summary>
        public JsonValue this[string key]
        {
            get
            {
                if (Kind != JsonKind.Object) return Null;
                return _index.TryGetValue(key, out int i) ? _members[i].Value : Null;
            }
        }

        /// <summary>Throws a readable error when a required member is missing.</summary>
        public JsonValue Require(string key)
        {
            Expect(JsonKind.Object);
            if (!_index.TryGetValue(key, out int i)) throw new JsonException($"Missing required field \"{key}\".");
            return _members[i].Value;
        }

        public double GetNumber(string key, double fallback) => this[key].Kind == JsonKind.Number ? this[key]._number : fallback;
        public int GetInt(string key, int fallback) => this[key].Kind == JsonKind.Number ? this[key].AsInt() : fallback;
        public string GetString(string key, string fallback) => this[key].Kind == JsonKind.String ? this[key]._string : fallback;
        public bool GetBool(string key, bool fallback) => this[key].Kind == JsonKind.Bool ? this[key]._bool : fallback;

        // ---- Building ----

        public JsonValue Add(JsonValue item)
        {
            Expect(JsonKind.Array);
            _items.Add(item ?? Null);
            return this;
        }

        public JsonValue Add(string item) => Add(From(item));
        public JsonValue Add(double item) => Add(From(item));

        /// <summary>Sets a member, replacing an existing one in place so the key order stays stable.</summary>
        public JsonValue Set(string key, JsonValue value)
        {
            Expect(JsonKind.Object);
            if (key == null) throw new ArgumentNullException(nameof(key));
            value = value ?? Null;
            if (_index.TryGetValue(key, out int i)) _members[i] = new KeyValuePair<string, JsonValue>(key, value);
            else
            {
                _index[key] = _members.Count;
                _members.Add(new KeyValuePair<string, JsonValue>(key, value));
            }
            return this;
        }

        public JsonValue Set(string key, string value) => Set(key, From(value));
        public JsonValue Set(string key, double value) => Set(key, From(value));
        public JsonValue Set(string key, bool value) => Set(key, From(value));

        private void Expect(JsonKind kind)
        {
            if (Kind != kind) throw new JsonException($"Expected JSON {kind.ToString().ToLowerInvariant()}, got {Kind.ToString().ToLowerInvariant()}.");
        }

        internal double RawNumber => _number;
        internal string RawString => _string;
        internal bool RawBool => _bool;
    }

    public sealed class JsonException : Exception
    {
        public JsonException(string message) : base(message) { }
    }
}
