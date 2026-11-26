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
        public const int CurrentSchemaVersion = 4;
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
        public RollerTuning Roller { get; }
        private readonly Dictionary<uint, PrizedReason> _prized = new Dictionary<uint, PrizedReason>();
        public IReadOnlyDictionary<StatTier, TierCap> TierCaps { get; }

        private BreedCatalog(int schemaVersion, double penalty, Dictionary<string, LearningRate> classRates,
            List<Breed> breeds, List<OtherModel> otherModels, RollerTuning roller, Dictionary<StatTier, TierCap> tierCaps)
        {
            Roller = roller;
            TierCaps = tierCaps;
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

        /// <summary>True when the model is on the sheet's Prized tab (special, rare or community favorite).</summary>
        public bool TryGetPrized(uint modelHash, out PrizedReason reason) => _prized.TryGetValue(modelHash, out reason);

        public bool TryGetPrized(string modelName, out PrizedReason reason) => _prized.TryGetValue(Joaat.Hash(modelName), out reason);

        public int PrizedModelCount => _prized.Count;

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

            RollerTuning roller = RollerTuning.Read(root["roller"], problems);
            var tierCaps = ReadTiers(root["tiers"], problems);

            var prized = new Dictionary<uint, PrizedReason>();
            JsonValue prizedArray = root["prizedModels"];
            if (prizedArray.Kind == JsonKind.Array)
            {
                foreach (JsonValue pm in prizedArray.Items)
                {
                    string model = pm.GetString("model", null);
                    if (string.IsNullOrWhiteSpace(model)) { problems.Add("A prizedModels entry has no \"model\"."); continue; }
                    if (!modelOwner.TryGetValue(model, out string owner) || owner == "otherModels")
                    {
                        problems.Add($"Prized model {model} is not a breed's model, so it would stay vanilla anyway.");
                        continue;
                    }
                    if (!NameMatching.TryParsePrizedReason(pm.GetString("why", ""), out PrizedReason reason))
                    {
                        problems.Add($"Prized model {model}: \"why\" must be one of {string.Join(", ", Enum.GetNames(typeof(PrizedReason)))}.");
                        continue;
                    }
                    uint h = Joaat.Hash(model.Trim().ToLowerInvariant());
                    if (prized.ContainsKey(h)) problems.Add($"Prized model {model} is listed twice.");
                    else prized[h] = reason;
                }
            }

            if (problems.Count > 0) throw new BreedDataException(problems);

            var catalog = new BreedCatalog(schema, penalty, classRates, breeds, others, roller, tierCaps);
            foreach (var kv in prized) catalog._prized[kv.Key] = kv.Value;
            log.Info(Log, $"Loaded {breeds.Count} breeds and {catalog.ModelCount} horse models ({others.Count} not tied to a breed, {prized.Count} prized).");
            return catalog;
        }

        private static Dictionary<StatTier, TierCap> ReadTiers(JsonValue j, List<string> problems)
        {
            var caps = new Dictionary<StatTier, TierCap>();
            if (j.Kind != JsonKind.Object)
            {
                problems.Add("\"tiers\" must be an object with one entry per tier (the Tiers tab).");
                return caps;
            }
            foreach (var kv in j.Members)
            {
                if (!NameMatching.TryParseTier(kv.Key, out StatTier tier)) { problems.Add($"tiers: unknown tier \"{kv.Key}\"."); continue; }
                double up = kv.Value.GetNumber("statUpRanks", double.NaN), down = kv.Value.GetNumber("statDownRanks", double.NaN);
                if (!IsRank(up) || !IsRank(down))
                    problems.Add($"tiers.{kv.Key}: statUpRanks and statDownRanks must be whole ranks from 0 to {TierCap.MaxRank}.");
                else caps[tier] = new TierCap(tier, (int)up, (int)down);
            }
            foreach (StatTier t in Enum.GetValues(typeof(StatTier)))
                if (!caps.ContainsKey(t) && j.Kind == JsonKind.Object && !problems.Exists(p => p.StartsWith("tiers." + t))) problems.Add($"tiers: no caps for {t}.");
            return caps;
        }

        private static bool IsRank(double v) => !double.IsNaN(v) && v == Math.Floor(v) && v >= 0 && v <= TierCap.MaxRank;

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

            string handling = j.GetString("handling", null);
            if (!NameMatching.TryParseTier(handling, out StatTier tier))
                problems.Add($"{name}: handling \"{handling}\" must be one of {string.Join(", ", Enum.GetNames(typeof(StatTier)))}.");

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

            return new Breed(name.Trim(), classes, tier, leans, quirks,
                j.GetString("demeanor", ""), models, learning, overridden);
        }
    }
}
