using System;
using System.Collections.Generic;
using System.Globalization;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Json;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Rolling;

namespace HorsePersonalities.Core.Saving
{
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// horses.json (Build plan: Data model): one record per permanent horse, keyed by fingerprint. Only permanent
    /// personalities are written. Skills join in Phase 8; a file without them reads fine.
    /// </summary>
    public static class HorseSaveFormat
    {
        public const int CurrentSchemaVersion = 1;
        private const string Log = "Save";

        public static string Write(IEnumerable<HorseRecord> records)
        {
            var horses = JsonValue.NewObject();
            var sorted = new List<HorseRecord>(records);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Id.Key, b.Id.Key)); // stable order: the file diffs cleanly
            foreach (HorseRecord r in sorted)
            {
                if (r.Status != PersonalityStatus.Permanent) continue;
                Personality p = r.Personality;
                var quirks = JsonValue.NewArray();
                foreach (Quirk q in p.Quirks) quirks.Add(q.ToString());
                horses.Set(r.Id.Key, JsonValue.NewObject()
                    .Set("breed", p.Breed)
                    .Set("origin", p.Origin.ToString())
                    .Set("status", r.Status.ToString())
                    .Set("quality", p.Quality.ToString())
                    .Set("traits", JsonValue.NewObject()
                        .Set("courage", Exact(p.Traits.Courage)).Set("nerves", Exact(p.Traits.Nerves))
                        .Set("temper", Exact(p.Traits.Temper)).Set("bondAffinity", Exact(p.Traits.BondAffinity)))
                    .Set("quirks", quirks)
                    .Set("naturalStats", JsonValue.NewObject()
                        .Set("speed", Exact(p.NaturalStats.Speed)).Set("acceleration", Exact(p.NaturalStats.Acceleration))
                        .Set("health", Exact(p.NaturalStats.Health)).Set("stamina", Exact(p.NaturalStats.Stamina)))
                    .Set("breaking", p.Breaking.ToString())
                    // A 64-bit seed doesn't fit a JSON number exactly, so it is stored as hex text.
                    .Set("seed", p.Seed.ToString("X16", CultureInfo.InvariantCulture))
                    .Set("rollerVersion", Personality.RollerVersion)
                    .Set("firstSeen", r.FirstSeen ?? "")
                    .Set("lastSeen", r.LastSeen ?? ""));
            }
            return JsonValue.NewObject().Set("schemaVersion", CurrentSchemaVersion).Set("horses", horses).ToJson();
        }

        /// <summary>
        /// Reads a whole file. Throws <see cref="SaveFormatException"/> if the file as a whole can't be trusted
        /// (not JSON, wrong schema). A single bad record is skipped with a log line, and the rest load.
        /// </summary>
        public static List<HorseRecord> Read(string text, ILog log)
        {
            log = log ?? NullLog.Instance;
            JsonValue root;
            try { root = JsonValue.Parse(text); }
            catch (JsonException ex) { throw new SaveFormatException("horses.json is not valid JSON: " + ex.Message); }

            int schema = root.GetInt("schemaVersion", -1);
            if (schema != CurrentSchemaVersion)
                throw new SaveFormatException($"horses.json has schemaVersion {schema}; this build reads {CurrentSchemaVersion}.");
            JsonValue horses = root["horses"];
            if (horses.Kind != JsonKind.Object) throw new SaveFormatException("horses.json has no \"horses\" object.");

            var records = new List<HorseRecord>();
            foreach (var kv in horses.Members)
            {
                try
                {
                    records.Add(ReadRecord(kv.Key, kv.Value, log));
                }
                catch (Exception ex) when (ex is SaveFormatException || ex is JsonException || ex is ArgumentException)
                {
                    log.Warn(Log, $"Skipping saved horse \"{kv.Key}\": {ex.Message}");
                }
            }
            return records;
        }

        private static HorseRecord ReadRecord(string key, JsonValue j, ILog log)
        {
            if (!HorseFingerprint.TryParse(key, out HorseFingerprint id)) throw new SaveFormatException("the key is not model|sex|coat.");
            string breed = j.GetString("breed", null);
            if (string.IsNullOrWhiteSpace(breed)) throw new SaveFormatException("no breed.");

            JsonValue t = j.Require("traits");
            var traits = new Traits(Trait(t, "courage"), Trait(t, "nerves"), Trait(t, "temper"), Trait(t, "bondAffinity"));

            var quirks = new List<Quirk>();
            foreach (JsonValue q in j["quirks"].Kind == JsonKind.Array ? j["quirks"].Items : Array.Empty<JsonValue>())
            {
                if (q.Kind == JsonKind.String && NameMatching.TryParseQuirk(q.AsString(), out Quirk quirk)) { if (!quirks.Contains(quirk)) quirks.Add(quirk); }
                else log.Warn(Log, $"{key}: skipping unknown quirk {q}.");
            }

            JsonValue s = j.Require("naturalStats");
            var stats = new NaturalStats(Stat(s, "speed"), Stat(s, "acceleration"), Stat(s, "health"), Stat(s, "stamina"));

            ulong seed = 0;
            string seedText = j.GetString("seed", null);
            if (seedText != null && !ulong.TryParse(seedText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed))
                throw new SaveFormatException($"seed \"{seedText}\" is not hex.");

            var p = new Personality(breed, Enum<HorseOrigin>(j, "origin", HorseOrigin.Unknown), seed,
                Enum<HorseQuality>(j, "quality", null), traits, quirks, stats, Enum<BreakingDifficulty>(j, "breaking", BreakingDifficulty.Normal));
            return new HorseRecord(id, p, PersonalityStatus.Permanent, j.GetString("firstSeen", ""), j.GetString("lastSeen", ""));
        }

        private static double Trait(JsonValue t, string name)
        {
            double v = t.Require(name).AsNumber();
            if (v < TraitLeans.Min || v > TraitLeans.Max) throw new SaveFormatException($"trait {name} = {v} is outside -2..+2.");
            return v;
        }

        private static double Stat(JsonValue s, string name)
        {
            double v = s.Require(name).AsNumber();
            if (v < -TierCap.MaxCap || v > TierCap.MaxCap) throw new SaveFormatException($"natural stat {name} = {v} is out of range.");
            return v;
        }

        private static T Enum<T>(JsonValue j, string key, T? fallback) where T : struct
        {
            string raw = j.GetString(key, null);
            if (raw != null && System.Enum.TryParse(raw, true, out T v) && System.Enum.IsDefined(typeof(T), v)) return v;
            if (fallback.HasValue && raw == null) return fallback.Value;
            throw new SaveFormatException($"{key} \"{raw}\" is not one of {string.Join(", ", System.Enum.GetNames(typeof(T)))}.");
        }

        /// <summary>Full precision: the writer uses the shortest text that reads back to the same number, so a reload is exact.</summary>
        private static double Exact(double v) => v;
    }
}
