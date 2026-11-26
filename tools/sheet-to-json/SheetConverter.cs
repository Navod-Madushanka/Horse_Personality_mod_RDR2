using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Json;

namespace HorsePersonalities.Tools.SheetToJson
{
    public sealed class ConversionException : Exception
    {
        public ConversionException(IEnumerable<string> problems) : base(string.Join(Environment.NewLine, problems)) { }
    }

    /// <summary>
    /// Builds breeds.json from three CSV files:
    ///   Breeds.csv   the sheet's Breeds tab (header row starts with "Breed")
    ///   Classes.csv  the sheet's Classes tab ("Multi-class penalty" row and a "Class" header row)
    ///   horse-models.csv  model,breed,note: every a_c_horse_ model, with the breed it belongs to (blank if none)
    ///   Roller.csv   the sheet's Roller tab (Setting, Value): the roller's tuning numbers
    ///   Tiers.csv    the sheet's Tiers tab (Tier, Stat up cap, Stat down cap)
    ///   Prized.csv   the sheet's Prized tab (Model, Why, Notes): special, rare and community-favorite models
    /// Columns are found by header name, so reordering or adding columns in the sheet is fine.
    /// </summary>
    public static class SheetConverter
    {
        public static JsonValue Convert(string breedsCsv, string classesCsv, string modelsCsv, string rollerCsv, string tiersCsv, string prizedCsv)
        {
            var problems = new List<string>();
            JsonValue roller = ReadRoller(rollerCsv, problems);
            JsonValue tiers = ReadTiers(tiersCsv, problems);

            // ---- Classes ----
            var classRows = Csv.Parse(classesCsv);
            double penalty = double.NaN;
            var classRates = new List<(string Name, double Physical, double Combat)>();
            int classHeader = classRows.FindIndex(r => Cell(r, 0).Equals("Class", StringComparison.OrdinalIgnoreCase));
            foreach (var r in classRows)
                if (Cell(r, 0).StartsWith("Multi-class penalty", StringComparison.OrdinalIgnoreCase)) penalty = ParseFraction(Cell(r, 1));
            if (double.IsNaN(penalty)) problems.Add("Classes.csv: no \"Multi-class penalty\" row with a value like 10%.");
            if (classHeader < 0) problems.Add("Classes.csv: no header row starting with \"Class\".");
            else
            {
                var h = Header(classRows[classHeader]);
                int pCol = Col(h, "Physical learning", problems, "Classes.csv");
                int cCol = Col(h, "Combat learning", problems, "Classes.csv");
                for (int i = classHeader + 1; i < classRows.Count; i++)
                {
                    string name = Cell(classRows[i], 0);
                    if (name.Length == 0) continue;
                    if (!TryNumber(Cell(classRows[i], pCol), out double p) || !TryNumber(Cell(classRows[i], cCol), out double c))
                    {
                        problems.Add($"Classes.csv: class {name} has a missing or bad rate.");
                        continue;
                    }
                    classRates.Add((name, p, c));
                }
            }
            var rateLookup = classRates.ToDictionary(c => c.Name, c => new LearningRate(c.Physical, c.Combat), StringComparer.OrdinalIgnoreCase);

            // ---- Models ----
            var modelRows = Csv.Parse(modelsCsv);
            var modelsByBreed = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var otherModels = new List<(string Model, string Note)>();
            var seenModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (modelRows.Count == 0) problems.Add("horse-models.csv is empty.");
            else
            {
                var h = Header(modelRows[0]);
                int mCol = Col(h, "model", problems, "horse-models.csv");
                int bCol = Col(h, "breed", problems, "horse-models.csv");
                h.TryGetValue("note", out int nCol);
                for (int i = 1; i < modelRows.Count; i++)
                {
                    string model = Cell(modelRows[i], mCol).Trim().ToLowerInvariant();
                    if (model.Length == 0) continue;
                    if (!seenModels.Add(model)) { problems.Add($"horse-models.csv: {model} is listed twice."); continue; }
                    string breed = Cell(modelRows[i], bCol).Trim();
                    if (breed.Length == 0) otherModels.Add((model, h.ContainsKey("note") ? Cell(modelRows[i], nCol).Trim() : ""));
                    else
                    {
                        if (!modelsByBreed.TryGetValue(breed, out var list)) modelsByBreed[breed] = list = new List<string>();
                        list.Add(model);
                    }
                }
            }

            // ---- Breeds ----
            var breedRows = Csv.Parse(breedsCsv);
            int header = breedRows.FindIndex(r => Cell(r, 0).Equals("Breed", StringComparison.OrdinalIgnoreCase));
            var breeds = JsonValue.NewArray();
            var breedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (header < 0) problems.Add("Breeds.csv: no header row starting with \"Breed\".");
            else
            {
                var h = Header(breedRows[header]);
                const string f = "Breeds.csv";
                int cClass = Col(h, "Class", problems, f), cClass2 = Col(h, "Class 2", problems, f);
                int cPhys = Col(h, "Physical learning", problems, f), cComb = Col(h, "Combat learning", problems, f);
                int cCour = Col(h, "Courage", problems, f), cNerv = Col(h, "Nerves", problems, f);
                int cTemp = Col(h, "Temper", problems, f), cBond = Col(h, "Bond affinity", problems, f);
                int cDem = Col(h, "Demeanor", problems, f), cQuirk = Col(h, "Likely quirks", problems, f);
                int cHand = Col(h, "Handling", problems, f);

                for (int i = header + 1; i < breedRows.Count; i++)
                {
                    var r = breedRows[i];
                    string name = Cell(r, 0).Trim();
                    if (name.Length == 0) continue;
                    string where = $"Breeds.csv row {i + 1} ({name})";
                    if (!breedNames.Add(name)) { problems.Add($"{where}: breed listed twice."); continue; }

                    var classes = new List<string>();
                    foreach (int col in new[] { cClass, cClass2 })
                    {
                        string c = Cell(r, col).Trim();
                        if (c.Length == 0) continue;
                        if (!rateLookup.ContainsKey(c)) problems.Add($"{where}: class \"{c}\" is not on the Classes tab.");
                        else classes.Add(rateLookup.Keys.First(k => k.Equals(c, StringComparison.OrdinalIgnoreCase)));
                    }
                    if (classes.Count == 0) { problems.Add($"{where}: needs a class."); continue; }

                    var leans = JsonValue.NewObject();
                    foreach (var (key, col, label) in new[] { ("courage", cCour, "Courage"), ("nerves", cNerv, "Nerves"), ("temper", cTemp, "Temper"), ("bondAffinity", cBond, "Bond affinity") })
                    {
                        string raw = Cell(r, col);
                        if (raw.Trim().Length == 0) { leans.Set(key, 0); continue; }
                        if (!TryNumber(raw, out double v) || v < -2 || v > 2) { problems.Add($"{where}: {label} \"{raw}\" must be a number from -2 to +2."); continue; }
                        leans.Set(key, v);
                    }

                    var quirks = JsonValue.NewArray();
                    foreach (string q in Cell(r, cQuirk).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (NameMatching.TryParseQuirk(q.Trim(), out Quirk quirk)) quirks.Add(quirk.ToString());
                        else problems.Add($"{where}: unknown quirk \"{q.Trim()}\". Known: {string.Join(", ", Enum.GetNames(typeof(Quirk)))}.");
                    }

                    string handling = Cell(r, cHand).Trim();
                    if (!NameMatching.TryParseTier(handling, out StatTier tier))
                        problems.Add($"{where}: Handling \"{handling}\" must be one of {string.Join(", ", Enum.GetNames(typeof(StatTier)))}. It is the breed's stat tier.");

                    modelsByBreed.TryGetValue(name, out var models);
                    models = models ?? new List<string>();
                    models.Sort(StringComparer.Ordinal);
                    var modelArray = JsonValue.NewArray();
                    foreach (string m in models) modelArray.Add(m);

                    var classArray = JsonValue.NewArray();
                    foreach (string c in classes) classArray.Add(c);

                    var breed = JsonValue.NewObject()
                        .Set("name", name)
                        .Set("models", modelArray)
                        .Set("classes", classArray)
                        .Set("handling", tier.ToString())
                        .Set("leans", leans)
                        .Set("likelyQuirks", quirks)
                        .Set("demeanor", Cell(r, cDem).Trim());

                    // The sheet computes learning rates from the Classes tab but lets a breed type over them.
                    // Only a real override goes into the JSON; Core computes the rest from the same formula.
                    if (!double.IsNaN(penalty) && classes.All(rateLookup.ContainsKey))
                    {
                        LearningRate computed = LearningRates.Compute(classes, rateLookup, penalty);
                        bool hasP = TryNumber(Cell(r, cPhys), out double sp);
                        bool hasC = TryNumber(Cell(r, cComb), out double sc);
                        // The CSV shows 2 decimals, so anything within half a hundredth is the formula's value.
                        bool overP = hasP && Math.Abs(sp - computed.Physical) > 0.0051;
                        bool overC = hasC && Math.Abs(sc - computed.Combat) > 0.0051;
                        if (overP || overC)
                            breed.Set("learning", JsonValue.NewObject()
                                .Set("physical", overP ? sp : Round(computed.Physical))
                                .Set("combat", overC ? sc : Round(computed.Combat)));
                    }
                    breeds.Add(breed);
                }
            }

            foreach (string b in modelsByBreed.Keys)
                if (!breedNames.Contains(b)) problems.Add($"horse-models.csv: breed \"{b}\" is not in Breeds.csv.");
            foreach (string b in breedNames)
                if (!modelsByBreed.ContainsKey(b)) problems.Add($"Breeds.csv: {b} has no models in horse-models.csv, so the mod could never recognize it.");

            JsonValue prizedModels = ReadPrized(prizedCsv, modelsByBreed, problems);
            if (problems.Count > 0) throw new ConversionException(problems);

            var classesJson = JsonValue.NewObject();
            foreach (var c in classRates)
                classesJson.Set(c.Name, JsonValue.NewObject().Set("physical", c.Physical).Set("combat", c.Combat));

            var others = JsonValue.NewArray();
            foreach (var o in otherModels.OrderBy(o => o.Model, StringComparer.Ordinal))
                others.Add(JsonValue.NewObject().Set("model", o.Model).Set("note", o.Note));

            return JsonValue.NewObject()
                .Set("schemaVersion", BreedCatalog.CurrentSchemaVersion)
                .Set("generatedBy", "tools/sheet-to-json from the breed sheet's CSV export. Edit the sheet, not this file.")
                .Set("multiClassPenalty", penalty)
                .Set("classes", classesJson)
                .Set("roller", roller)
                .Set("tiers", tiers)
                .Set("breeds", breeds)
                .Set("otherModels", others)
                .Set("prizedModels", prizedModels);
        }

        private static JsonValue ReadRoller(string csv, List<string> problems)
        {
            var rows = Csv.Parse(csv);
            var result = JsonValue.NewObject();
            int header = rows.FindIndex(r => Cell(r, 0).Equals("Setting", StringComparison.OrdinalIgnoreCase));
            if (header < 0) { problems.Add("Roller.csv: no header row starting with \"Setting\"."); return result; }
            int vCol = Col(Header(rows[header]), "Value", problems, "Roller.csv");
            var seen = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            for (int i = header + 1; i < rows.Count; i++)
            {
                string label = Cell(rows[i], 0).Trim();
                if (label.Length == 0) continue;
                var field = RollerTuning.Fields.FirstOrDefault(f => f.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
                if (field.Key == null) { problems.Add($"Roller.csv: unknown setting \"{label}\". Known: {string.Join(", ", RollerTuning.Fields.Select(f => f.Label))}."); continue; }
                string raw = Cell(rows[i], vCol);
                double v = raw.Trim().EndsWith("%", StringComparison.Ordinal) ? ParseFraction(raw) : (TryNumber(raw, out double n) ? n : double.NaN);
                if (double.IsNaN(v)) { problems.Add($"Roller.csv: {label} \"{raw}\" is not a number."); continue; }
                seen[field.Key] = v;
            }
            // Written in the Fields order, so the JSON doesn't change when rows move in the sheet.
            foreach (var f in RollerTuning.Fields)
            {
                if (seen.TryGetValue(f.Key, out double v)) result.Set(f.Key, Round(v));
                else problems.Add($"Roller.csv: no \"{f.Label}\" row.");
            }
            return result;
        }

        private static JsonValue ReadPrized(string csv, Dictionary<string, List<string>> modelsByBreed, List<string> problems)
        {
            var result = JsonValue.NewArray();
            var rows = Csv.Parse(csv ?? "");
            int header = rows.FindIndex(r => Cell(r, 0).Equals("Model", StringComparison.OrdinalIgnoreCase));
            if (header < 0) { problems.Add("Prized.csv: no header row starting with \"Model\"."); return result; }
            var h = Header(rows[header]);
            int whyCol = Col(h, "Why", problems, "Prized.csv");
            h.TryGetValue("Notes", out int noteCol);
            var breedModels = new HashSet<string>(modelsByBreed.Values.SelectMany(m => m), StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<(string Model, string Why, string Note)>();
            for (int i = header + 1; i < rows.Count; i++)
            {
                string model = Cell(rows[i], 0).Trim().ToLowerInvariant();
                if (model.Length == 0) continue;
                string where = $"Prized.csv row {i + 1} ({model})";
                if (!breedModels.Contains(model)) { problems.Add($"{where}: not a breed's model in horse-models.csv, so it would stay vanilla anyway."); continue; }
                if (!seen.Add(model)) { problems.Add($"{where}: listed twice."); continue; }
                if (!NameMatching.TryParsePrizedReason(Cell(rows[i], whyCol), out PrizedReason why))
                {
                    problems.Add($"{where}: Why \"{Cell(rows[i], whyCol)}\" must be Special, Rare or Community favorite.");
                    continue;
                }
                entries.Add((model, why.ToString(), h.ContainsKey("Notes") ? Cell(rows[i], noteCol).Trim() : ""));
            }
            foreach (var e in entries.OrderBy(e => e.Model, StringComparer.Ordinal))
                result.Add(JsonValue.NewObject().Set("model", e.Model).Set("why", e.Why).Set("note", e.Note));
            return result;
        }

        private static JsonValue ReadTiers(string csv, List<string> problems)
        {
            var rows = Csv.Parse(csv);
            var result = JsonValue.NewObject();
            int header = rows.FindIndex(r => Cell(r, 0).Equals("Tier", StringComparison.OrdinalIgnoreCase));
            if (header < 0) { problems.Add("Tiers.csv: no header row starting with \"Tier\"."); return result; }
            var h = Header(rows[header]);
            int upCol = Col(h, "Stat up cap", problems, "Tiers.csv"), downCol = Col(h, "Stat down cap", problems, "Tiers.csv");
            var found = new Dictionary<StatTier, (int Up, int Down)>();
            for (int i = header + 1; i < rows.Count; i++)
            {
                string name = Cell(rows[i], 0).Trim();
                if (name.Length == 0) continue;
                if (!NameMatching.TryParseTier(name, out StatTier tier)) { problems.Add($"Tiers.csv: unknown tier \"{name}\"."); continue; }
                // Caps are whole ranks on the game's 0-9 scale (decided Nov 25, 2026).
                if (!TryRank(Cell(rows[i], upCol), out int up) || !TryRank(Cell(rows[i], downCol), out int down))
                {
                    problems.Add($"Tiers.csv: {name} needs both caps as whole ranks from 0 to {TierCap.MaxRank}, like 1.");
                    continue;
                }
                found[tier] = (up, down);
            }
            foreach (StatTier t in Enum.GetValues(typeof(StatTier)))
            {
                if (found.TryGetValue(t, out var c)) result.Set(t.ToString(), JsonValue.NewObject().Set("statUpRanks", c.Up).Set("statDownRanks", c.Down));
                else problems.Add($"Tiers.csv: no row for {t}.");
            }
            return result;
        }

        private static bool TryRank(string raw, out int rank)
        {
            rank = 0;
            if (!TryNumber(raw, out double v) || v != Math.Floor(v) || v < 0 || v > TierCap.MaxRank) return false;
            rank = (int)v;
            return true;
        }

        private static double Round(double v) => Math.Round(v, 4);

        private static string Cell(List<string> row, int col) => col >= 0 && col < row.Count ? row[col] ?? "" : "";

        private static Dictionary<string, int> Header(List<string> row)
        {
            var h = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < row.Count; i++)
            {
                string k = row[i].Trim();
                if (k.Length > 0 && !h.ContainsKey(k)) h[k] = i;
            }
            return h;
        }

        private static int Col(Dictionary<string, int> header, string name, List<string> problems, string file)
        {
            if (header.TryGetValue(name, out int i)) return i;
            problems.Add($"{file}: no \"{name}\" column.");
            return -1;
        }

        /// <summary>Reads "+1", "-2", "0.90", "1,5" (decimal comma) and "1.1700000000000002".</summary>
        public static bool TryNumber(string raw, out double v)
        {
            string s = (raw ?? "").Trim().Replace('−', '-');
            if (s.StartsWith("+", StringComparison.Ordinal)) s = s.Substring(1);
            if (s.Count(ch => ch == ',') == 1 && !s.Contains('.')) s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);
        }

        /// <summary>"10%" -> 0.10; "0.1" -> 0.1.</summary>
        public static double ParseFraction(string raw)
        {
            string s = (raw ?? "").Trim();
            bool percent = s.EndsWith("%", StringComparison.Ordinal);
            if (percent) s = s.TrimEnd('%');
            if (!TryNumber(s, out double v)) return double.NaN;
            return percent || v > 1 ? v / 100.0 : v;
        }
    }
}
