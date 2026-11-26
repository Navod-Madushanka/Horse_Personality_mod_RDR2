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

    /// <summary>Natural stats for a horse saved before they were whole ranks: re-rolled from its saved seed.</summary>
    public delegate NaturalStats StatsUpgrade(string breed, HorseOrigin origin, ulong seed);

    /// <summary>
    /// horses.json (Build plan: Data model): one record per permanent horse, keyed by fingerprint. Only permanent
    /// personalities are written. Skills join in Phase 8; a file without them reads fine.
    /// Schema 1 (natural stats as fractions) is upgraded on read to schema 2 (whole ranks).
    /// </summary>
    public static class HorseSaveFormat
    {
        public const int CurrentSchemaVersion = 2;
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
                        .Set("speed", p.NaturalStats.Speed).Set("acceleration", p.NaturalStats.Acceleration)
                        .Set("health", p.NaturalStats.Health).Set("stamina", p.NaturalStats.Stamina))
                    .Set("breaking", p.Breaking.ToString())
                    .Set("prized", p.Prized)
                    .Set("vanillaBase", VanillaJson(r))
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
        public static List<HorseRecord> Read(string text, ILog log, StatsUpgrade upgrade = null) => Read(text, log, upgrade, out _);

        /// <param name="upgraded">True when the file was an older schema and was upgraded; write it back to keep the upgrade.</param>
        public static List<HorseRecord> Read(string text, ILog log, StatsUpgrade upgrade, out bool upgraded)
        {
            log = log ?? NullLog.Instance;
            upgraded = false;
            JsonValue root;
            try { root = JsonValue.Parse(text); }
            catch (JsonException ex) { throw new SaveFormatException("horses.json is not valid JSON: " + ex.Message); }

            int schema = root.GetInt("schemaVersion", -1);
            if (schema != 1 && schema != CurrentSchemaVersion)
                throw new SaveFormatException($"horses.json has schemaVersion {schema}; this build reads 1 and {CurrentSchemaVersion}.");
            upgraded = schema == 1;
            JsonValue horses = root["horses"];
            if (horses.Kind != JsonKind.Object) throw new SaveFormatException("horses.json has no \"horses\" object.");

            var records = new List<HorseRecord>();
            foreach (var kv in horses.Members)
            {
                try
                {
                    records.Add(ReadRecord(kv.Key, kv.Value, log, schema, upgrade));
                }
                catch (Exception ex) when (ex is SaveFormatException || ex is JsonException || ex is ArgumentException)
                {
                    log.Warn(Log, $"Skipping saved horse \"{kv.Key}\": {ex.Message}");
                }
            }
            if (upgraded) log.Info(Log, $"Upgraded horses.json from schema 1 to {CurrentSchemaVersion}: natural stats are whole ranks now, re-rolled from each horse's seed.");
            return records;
        }

        private static HorseRecord ReadRecord(string key, JsonValue j, ILog log, int schema, StatsUpgrade upgrade)
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

            ulong seed = 0;
            string seedText = j.GetString("seed", null);
            if (seedText != null && !ulong.TryParse(seedText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed))
                throw new SaveFormatException($"seed \"{seedText}\" is not hex.");
            HorseOrigin origin = Enum<HorseOrigin>(j, "origin", HorseOrigin.Unknown);

            NaturalStats stats;
            if (schema == 1)
            {
                // Fractions of the baseline can't be turned into ranks fairly, so roll them again from the horse's seed.
                if (upgrade != null && seedText != null) stats = upgrade(breed, origin, seed);
                else
                {
                    stats = new NaturalStats(0, 0, 0, 0);
                    log.Warn(Log, $"{key}: saved before natural stats were ranks, with no way to re-roll them; set to 0.");
                }
            }
            else
            {
                JsonValue s = j.Require("naturalStats");
                stats = new NaturalStats(Stat(s, "speed"), Stat(s, "acceleration"), Stat(s, "health"), Stat(s, "stamina"));
            }
            if (quirks.Count == 0) log.Warn(Log, $"{key}: has no quirks left (Show-off was cut); it keeps its other traits.");

            var p = new Personality(breed, origin, seed,
                Enum<HorseQuality>(j, "quality", null), traits, quirks, stats, Enum<BreakingDifficulty>(j, "breaking", BreakingDifficulty.Normal), j.GetBool("prized", false));
            var record = new HorseRecord(id, p, PersonalityStatus.Permanent, j.GetString("firstSeen", ""), j.GetString("lastSeen", ""));
            JsonValue vb = j["vanillaBase"];
            if (vb.Kind == JsonKind.Object)
                foreach (var kv in vb.Members)
                {
                    int v = kv.Value.AsInt();
                    if (v < 0 || v > TierCap.MaxRank) throw new SaveFormatException($"vanillaBase.{kv.Key} = {v} is not a rank.");
                    record.VanillaBase[kv.Key] = v;
                }
            return record;
        }

        private static JsonValue VanillaJson(HorseRecord r)
        {
            var o = JsonValue.NewObject();
            var keys = new List<string>(r.VanillaBase.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string k in keys) o.Set(k, r.VanillaBase[k]);
            return o;
        }

        private static double Trait(JsonValue t, string name)
        {
            double v = t.Require(name).AsNumber();
            if (v < TraitLeans.Min || v > TraitLeans.Max) throw new SaveFormatException($"trait {name} = {v} is outside -2..+2.");
            return v;
        }

        private static int Stat(JsonValue s, string name)
        {
            int v = s.Require(name).AsInt();
            if (v < -TierCap.MaxRank || v > TierCap.MaxRank) throw new SaveFormatException($"natural stat {name} = {v} ranks is out of range.");
            return v;
        }

        /// <summary>Full precision: the writer uses the shortest text that reads back to the same number, so a reload is exact.</summary>
        private static double Exact(double v) => v;

        private static T Enum<T>(JsonValue j, string key, T? fallback) where T : struct
        {
            string raw = j.GetString(key, null);
            if (raw != null && System.Enum.TryParse(raw, true, out T v) && System.Enum.IsDefined(typeof(T), v)) return v;
            if (fallback.HasValue && raw == null) return fallback.Value;
            throw new SaveFormatException($"{key} \"{raw}\" is not one of {string.Join(", ", System.Enum.GetNames(typeof(T)))}.");
        }


    }
}
