using System;
using System.Collections.Generic;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Json;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Testing
{
    /// <summary>A fixed personality for test mode (Build plan: Test mode).</summary>
    public sealed class TestPreset
    {
        public string Name { get; }
        public string Purpose { get; }
        private readonly HorseQuality _quality;
        private readonly bool _prized;
        private readonly Traits _traits;
        private readonly IReadOnlyList<Quirk> _quirks;
        private readonly NaturalStats _stats;
        private readonly BreakingDifficulty _breaking;
        private readonly HorseOrigin? _origin;

        internal TestPreset(string name, string purpose, HorseQuality quality, bool prized, Traits traits, IReadOnlyList<Quirk> quirks,
            NaturalStats stats, BreakingDifficulty breaking, HorseOrigin? origin)
        {
            Name = name; Purpose = purpose; _quality = quality; _prized = prized; _traits = traits; _quirks = quirks;
            _stats = stats; _breaking = breaking; _origin = origin;
        }

        /// <summary>The preset on a horse of this breed. The preset's origin wins (Wild presets make breaking testable on any horse).</summary>
        public Personality For(Breed breed, HorseOrigin origin, ulong seed) =>
            new Personality(breed.Name, _origin ?? origin, seed, _quality, _traits, _quirks, _stats, _breaking, _prized);
    }

    /// <summary>Loads HorsePersonalities/test-personalities.json: the presets and which models get which.</summary>
    public sealed class TestPresets
    {
        public IReadOnlyList<TestPreset> Presets { get; }
        private readonly Dictionary<string, string> _assign;

        private TestPresets(List<TestPreset> presets, Dictionary<string, string> assign)
        {
            Presets = presets;
            _assign = assign;
        }

        public TestPreset Find(string name)
        {
            foreach (TestPreset p in Presets) if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        /// <summary>The preset for a model: its entry in "assign", else the first preset (Average).</summary>
        public TestPreset ForModel(string model) =>
            _assign.TryGetValue(model ?? "", out string name) && Find(name) != null ? Find(name) : Presets[0];

        /// <summary>The preset after <paramref name="current"/>, wrapping around.</summary>
        public TestPreset Next(string current)
        {
            for (int i = 0; i < Presets.Count; i++)
                if (string.Equals(Presets[i].Name, current, StringComparison.OrdinalIgnoreCase)) return Presets[(i + 1) % Presets.Count];
            return Presets[0];
        }

        public static TestPresets Load(string json, ILog log)
        {
            var problems = new List<string>();
            JsonValue root;
            try { root = JsonValue.Parse(json); }
            catch (JsonException ex) { throw new BreedDataException(new[] { "test-personalities.json is not valid JSON: " + ex.Message }); }

            var presets = new List<TestPreset>();
            foreach (JsonValue p in root["presets"].Kind == JsonKind.Array ? root["presets"].Items : Array.Empty<JsonValue>())
            {
                string name = p.GetString("name", null);
                if (string.IsNullOrWhiteSpace(name)) { problems.Add("A preset has no name."); continue; }
                try
                {
                    JsonValue t = p.Require("traits");
                    double T(string k)
                    {
                        double v = t.GetNumber(k, 0);
                        if (v < TraitLeans.Min || v > TraitLeans.Max) throw new JsonException($"trait {k} = {v} is outside -2..+2.");
                        return v;
                    }
                    var quirks = new List<Quirk>();
                    foreach (JsonValue q in p["quirks"].Kind == JsonKind.Array ? p["quirks"].Items : Array.Empty<JsonValue>())
                        if (q.Kind == JsonKind.String && NameMatching.TryParseQuirk(q.AsString(), out Quirk quirk)) quirks.Add(quirk);
                        else throw new JsonException($"unknown quirk {q}.");
                    JsonValue s = p["naturalStats"];
                    var stats = new NaturalStats(s.GetInt("speed", 0), s.GetInt("acceleration", 0), s.GetInt("health", 0), s.GetInt("stamina", 0));
                    HorseOrigin? origin = null;
                    if (p.Has("origin"))
                    {
                        if (!Enum.TryParse(p.GetString("origin", ""), true, out HorseOrigin o)) throw new JsonException("origin must be Stable, Wild or Unknown.");
                        origin = o;
                    }
                    if (!Enum.TryParse(p.GetString("quality", "Good"), true, out HorseQuality quality)) throw new JsonException("quality must be Good or Bad.");
                    if (!Enum.TryParse(p.GetString("breaking", "Normal"), true, out BreakingDifficulty breaking)) throw new JsonException("breaking must be Normal, Hard or Unbreakable.");
                    presets.Add(new TestPreset(name, p.GetString("purpose", ""), quality, p.GetBool("prized", false),
                        new Traits(T("courage"), T("nerves"), T("temper"), T("bondAffinity")), quirks, stats, breaking, origin));
                }
                catch (JsonException ex) { problems.Add($"Preset {name}: {ex.Message}"); }
            }
            if (presets.Count == 0) problems.Add("test-personalities.json has no presets.");

            var assign = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root["assign"].Kind == JsonKind.Object)
                foreach (var kv in root["assign"].Members)
                {
                    string preset = kv.Value.Kind == JsonKind.String ? kv.Value.AsString() : null;
                    if (preset == null || presets.Find(x => string.Equals(x.Name, preset, StringComparison.OrdinalIgnoreCase)) == null)
                        problems.Add($"assign: {kv.Key} names no preset \"{preset}\".");
                    else assign[kv.Key.Trim().ToLowerInvariant()] = preset;
                }

            if (problems.Count > 0) throw new BreedDataException(problems);
            log?.Info("TestMode", $"Loaded {presets.Count} test presets, {assign.Count} model assignments.");
            return new TestPresets(presets, assign);
        }
    }
}
