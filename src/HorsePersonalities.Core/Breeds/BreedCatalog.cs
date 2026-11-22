using System;
using System.Collections.Generic;
using System.Linq;
using HorsePersonalities.Core.Hashing;
using HorsePersonalities.Core.Json;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Core.Breeds
{
    public sealed class BreedDataException : Exception
    {
        public IReadOnlyList<string> Problems { get; }

        public BreedDataException(IReadOnlyList<string> problems)
            : base("breeds.json has problems:\n- " + string.Join("\n- ", problems))
        {
            Problems = problems;
        }
    }

    /// <summary>A horse model the game has but no breed row covers (gang horses, mules, Online-only breeds).</summary>
    public sealed class OtherModel
    {
        public string Model { get; }
        public string Note { get; }

        public OtherModel(string model, string note)
        {
            Model = model;
            Note = note;
        }
    }

    /// <summary>
    /// Read-only design data from breeds.json, generated from the breed sheet. Never hardcode breed data
    /// anywhere else (Rules for the coding agent, rule 6).
    /// </summary>
    public sealed class BreedCatalog
    {
        public const int CurrentSchemaVersion = 1;
        private const string Log = "Breeds";

        private readonly Dictionary<string, Breed> _byName;
        private readonly Dictionary<uint, Breed> _byModelHash;
        private readonly Dictionary<uint, string> _modelNames;
        private readonly Dictionary<uint, OtherModel> _otherByHash;

        public int SchemaVersion { get; }
        public double MultiClassPenalty { get; }
        public IReadOnlyDictionary<string, LearningRate> ClassRates { get; }
        public IReadOnlyList<Breed> Breeds { get; }
        public IReadOnlyList<OtherModel> OtherModels { get; }

        private BreedCatalog(int schemaVersion, double penalty, Dictionary<string, LearningRate> classRates,
            List<Breed> breeds, List<OtherModel> otherModels)
        {
            SchemaVersion = schemaVersion;
            MultiClassPenalty = penalty;
            ClassRates = classRates;
            Breeds = breeds;
            OtherModels = otherModels;
            _byName = breeds.ToDictionary(b => NameMatching.Normalize(b.Name));
            _byModelHash = new Dictionary<uint, Breed>();
            _modelNames = new Dictionary<uint, string>();
            _otherByHash = new Dictionary<uint, OtherModel>();
            foreach (Breed b in breeds)
            {
                foreach (string m in b.Models)
                {
                    uint h = Joaat.Hash(m);
                    _byModelHash[h] = b;
                    _modelNames[h] = m;
                }
            }
            foreach (OtherModel o in otherModels)
            {
                uint h = Joaat.Hash(o.Model);
                _otherByHash[h] = o;
                _modelNames[h] = o.Model;
            }
        }

        public bool TryGetBreed(string name, out Breed breed) => _byName.TryGetValue(NameMatching.Normalize(name), out breed);

        /// <summary>Breed for a model hash as returned by GET_ENTITY_MODEL.</summary>
        public bool TryGetBreedByModel(uint modelHash, out Breed breed) => _byModelHash.TryGetValue(modelHash, out breed);

        public bool TryGetOtherModel(uint modelHash, out OtherModel other) => _otherByHash.TryGetValue(modelHash, out other);

        /// <summary>Model name for a hash, if breeds.json lists it anywhere; otherwise the hash in hex.</summary>
        public string DescribeModel(uint modelHash) =>
            _modelNames.TryGetValue(modelHash, out string name) ? name : $"0x{modelHash:X8}";

        public bool IsKnownModel(uint modelHash) => _modelNames.ContainsKey(modelHash);

        public int ModelCount => _modelNames.Count;

        /// <summary>
        /// Parses and validates breeds.json. Fatal problems (bad schema, unknown class, a model in two breeds,
        /// leanings off the scale) throw <see cref="BreedDataException"/> listing all of them at once.
        /// Minor problems (an unknown quirk name, a breed with no models) are logged and skipped.
        /// </summary>
        public static BreedCatalog Load(string json, ILog log)
        {
            log = log ?? NullLog.Instance;
            JsonValue root;
            try
            {
                root = JsonValue.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new BreedDataException(new[] { "Not valid JSON: " + ex.Message });
            }

            var problems = new List<string>();
            int schema = root.GetInt("schemaVersion", -1);
            if (schema != CurrentSchemaVersion)
            {
                problems.Add($"schemaVersion is {schema}; this build of the mod reads {CurrentSchemaVersion}.");
                throw new BreedDataException(problems);
            }

            double penalty = root.GetNumber("multiClassPenalty", double.NaN);
            if (double.IsNaN(penalty) || penalty < 0 || penalty >= 1)
                problems.Add("multiClassPenalty must be a number from 0 up to (not including) 1.");

            var classRates = new Dictionary<string, LearningRate>(StringComparer.OrdinalIgnoreCase);
            JsonValue classes = root["classes"];
            if (classes.Kind != JsonKind.Object || classes.Count == 0) problems.Add("\"classes\" must be an object with at least one class.");
            else
            {
                foreach (var kv in classes.Members)
                {
                    double p = kv.Value.GetNumber("physical", double.NaN);
                    double c = kv.Value.GetNumber("combat", double.NaN);
                    if (double.IsNaN(p) || double.IsNaN(c) || p <= 0 || c <= 0)
                        problems.Add($"Class \"{kv.Key}\" needs positive \"physical\" and \"combat\" rates.");
                    else classRates[kv.Key] = new LearningRate(p, c);
                }
            }

            var breeds = new List<Breed>();
            var modelOwner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var breedNames = new HashSet<string>();
            JsonValue breedArray = root["breeds"];
            if (breedArray.Kind != JsonKind.Array || breedArray.Count == 0) problems.Add("\"breeds\" must be a non-empty array.");
            else
            {
                for (int i = 0; i < breedArray.Count; i++)
                {
                    Breed b = ReadBreed(breedArray.Items[i], i, classRates, double.IsNaN(penalty) ? 0 : penalty, problems, log);
                    if (b == null) continue;
                    if (!breedNames.Add(NameMatching.Normalize(b.Name))) problems.Add($"Breed \"{b.Name}\" is listed twice.");
                    foreach (string m in b.Models)
                    {
                        if (modelOwner.TryGetValue(m, out string other)) problems.Add($"Model {m} is in both {other} and {b.Name}.");
                        else modelOwner[m] = b.Name;
                    }
                    if (b.Models.Count == 0) log.Warn(Log, $"{b.Name} has no models, so the mod can never recognize one in game.");
                    breeds.Add(b);
                }
            }

            var others = new List<OtherModel>();
            JsonValue otherArray = root["otherModels"];
            if (otherArray.Kind == JsonKind.Array)
            {
                foreach (JsonValue o in otherArray.Items)
                {
                    string model = o.GetString("model", null);
                    if (string.IsNullOrWhiteSpace(model)) { problems.Add("An otherModels entry has no \"model\"."); continue; }
                    if (modelOwner.TryGetValue(model, out string owner)) { problems.Add($"Model {model} is in both {owner} and otherModels."); continue; }
                    modelOwner[model] = "otherModels";
                    others.Add(new OtherModel(model, o.GetString("note", "")));
                }
            }

            if (problems.Count > 0) throw new BreedDataException(problems);

            var catalog = new BreedCatalog(schema, penalty, classRates, breeds, others);
            log.Info(Log, $"Loaded {breeds.Count} breeds and {catalog.ModelCount} horse models ({others.Count} not tied to a breed).");
            return catalog;
        }

        private static Breed ReadBreed(JsonValue j, int index, Dictionary<string, LearningRate> classRates, double penalty,
            List<string> problems, ILog log)
        {
            string name = j.GetString("name", null);
            if (string.IsNullOrWhiteSpace(name))
            {
                problems.Add($"Breed #{index + 1} has no name.");
                return null;
            }

            var classes = new List<string>();
            foreach (JsonValue c in j["classes"].Kind == JsonKind.Array ? j["classes"].Items : Array.Empty<JsonValue>())
            {
                if (c.Kind != JsonKind.String) continue;
                if (!classRates.ContainsKey(c.AsString())) problems.Add($"{name}: unknown class \"{c.AsString()}\".");
                else classes.Add(c.AsString());
            }
            if (classes.Count == 0)
            {
                problems.Add($"{name}: needs at least one known class.");
                return null;
            }

            JsonValue l = j["leans"];
            var leans = new TraitLeans(l.GetNumber("courage", 0), l.GetNumber("nerves", 0), l.GetNumber("temper", 0), l.GetNumber("bondAffinity", 0));
            foreach (var (label, v) in new[] { ("courage", leans.Courage), ("nerves", leans.Nerves), ("temper", leans.Temper), ("bondAffinity", leans.BondAffinity) })
                if (!TraitLeans.InRange(v)) problems.Add($"{name}: lean {label} = {v} is outside -2..+2.");

            var quirks = new List<Quirk>();
            foreach (JsonValue q in j["likelyQuirks"].Kind == JsonKind.Array ? j["likelyQuirks"].Items : Array.Empty<JsonValue>())
            {
                if (q.Kind == JsonKind.String && NameMatching.TryParseQuirk(q.AsString(), out Quirk quirk)) { if (!quirks.Contains(quirk)) quirks.Add(quirk); }
                else log.Warn(Log, $"{name}: skipping unknown quirk {q}.");
            }

            var models = new List<string>();
            foreach (JsonValue m in j["models"].Kind == JsonKind.Array ? j["models"].Items : Array.Empty<JsonValue>())
                if (m.Kind == JsonKind.String && !string.IsNullOrWhiteSpace(m.AsString())) models.Add(m.AsString().Trim().ToLowerInvariant());

            LearningRate computed = LearningRates.Compute(classes, classRates, penalty);
            LearningRate learning = computed;
            bool overridden = false;
            JsonValue lo = j["learning"];
            if (lo.Kind == JsonKind.Object)
            {
                learning = new LearningRate(lo.GetNumber("physical", computed.Physical), lo.GetNumber("combat", computed.Combat));
                overridden = !learning.ApproximatelyEquals(computed, 1e-4);
                if (learning.Physical <= 0 || learning.Combat <= 0) problems.Add($"{name}: learning rates must be positive.");
            }

            return new Breed(name.Trim(), classes, j.GetString("handling", "Standard"), leans, quirks,
                j.GetString("demeanor", ""), models, learning, overridden);
        }
    }
}
