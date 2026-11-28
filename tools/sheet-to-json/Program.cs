using System;
using System.IO;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Tools.SheetToJson
{
    /// <summary>
    /// Usage (from the repo root):
    ///   dotnet run --project tools/sheet-to-json
    ///   dotnet run --project tools/sheet-to-json -- --check     (fails if data/breeds.json is out of date)
    /// Options: --breeds PATH --classes PATH --models PATH --roller PATH --tiers PATH --out PATH
    /// Defaults: data/sheet-export/{Breeds,Classes,Roller,Tiers}.csv, data/horse-models.csv, data/breeds.json
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string root = FindRepoRoot();
            string breeds = Path.Combine(root, "data", "sheet-export", "Breeds.csv");
            string classes = Path.Combine(root, "data", "sheet-export", "Classes.csv");
            string models = Path.Combine(root, "data", "horse-models.csv");
            string roller = Path.Combine(root, "data", "sheet-export", "Roller.csv");
            string tiers = Path.Combine(root, "data", "sheet-export", "Tiers.csv");
            string prized = Path.Combine(root, "data", "sheet-export", "Prized.csv");
            string behavior = Path.Combine(root, "data", "sheet-export", "Behavior.csv");
            string predators = Path.Combine(root, "data", "predator-models.csv");
            string dogs = Path.Combine(root, "data", "dog-models.csv");
            string skills = Path.Combine(root, "data", "sheet-export", "Skills.csv");
            string demeanor = Path.Combine(root, "data", "sheet-export", "Demeanor.csv");
            string coats = Path.Combine(root, "data", "sheet-export", "Coats.csv");
            string treats = Path.Combine(root, "data", "treats.csv");
            string output = Path.Combine(root, "data", "breeds.json");
            bool check = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{a} needs a path.");
                switch (a)
                {
                    case "--breeds": breeds = Next(); break;
                    case "--classes": classes = Next(); break;
                    case "--models": models = Next(); break;
                    case "--roller": roller = Next(); break;
                    case "--tiers": tiers = Next(); break;
                    case "--prized": prized = Next(); break;
                    case "--behavior": behavior = Next(); break;
                    case "--predators": predators = Next(); break;
                    case "--dogs": dogs = Next(); break;
                    case "--skills": skills = Next(); break;
                    case "--demeanor": demeanor = Next(); break;
                    case "--coats": coats = Next(); break;
                    case "--treats": treats = Next(); break;
                    case "--out": output = Next(); break;
                    case "--check": check = true; break;
                    case "--default-ini":
                        // Phase 9: the release zip ships the INI the mod would write on first run.
                        string iniPath = Next();
                        File.WriteAllText(iniPath, HorsePersonalities.Core.Settings.ModSettings.DefaultIniText());
                        Console.WriteLine($"Wrote {iniPath}.");
                        return 0;
                    case "-h": case "--help":
                        Console.WriteLine("sheet-to-json [--breeds Breeds.csv] [--classes Classes.csv] [--models horse-models.csv] [--roller Roller.csv] [--tiers Tiers.csv] [--prized Prized.csv] [--behavior Behavior.csv] [--predators predator-models.csv] [--dogs dog-models.csv] [--skills Skills.csv] [--demeanor Demeanor.csv] [--coats Coats.csv] [--treats treats.csv] [--out breeds.json] [--check] | --default-ini HorsePersonalities.ini");
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown option {a}. Try --help.");
                        return 2;
                }
            }

            try
            {
                var json = SheetConverter.Convert(File.ReadAllText(breeds), File.ReadAllText(classes), File.ReadAllText(models), File.ReadAllText(roller), File.ReadAllText(tiers), File.ReadAllText(prized),
                    File.ReadAllText(behavior), File.ReadAllText(predators), File.ReadAllText(demeanor),
                    File.ReadAllText(coats), File.ReadAllText(treats), File.ReadAllText(dogs), File.ReadAllText(skills));
                string text = json.ToJson();

                // Same validation the mod runs on load, so a file that passes here loads in game.
                var log = new MemoryLog();
                var catalog = BreedCatalog.Load(text, log);
                foreach (var e in log.Entries) if (e.Level >= LogLevel.Warn) Console.Error.WriteLine($"warning: {e.Message}");

                if (check)
                {
                    string existing = File.Exists(output) ? File.ReadAllText(output) : "";
                    if (Normalize(existing) != Normalize(text))
                    {
                        Console.Error.WriteLine($"{output} is out of date. Run: dotnet run --project tools/sheet-to-json");
                        return 1;
                    }
                    Console.WriteLine($"{output} is up to date ({catalog.Breeds.Count} breeds, {catalog.ModelCount} models).");
                    return 0;
                }

                File.WriteAllText(output, text);
                Console.WriteLine($"Wrote {output}: {catalog.Breeds.Count} breeds, {catalog.ModelCount} models, {catalog.OtherModels.Count} not tied to a breed.");
                return 0;
            }
            catch (ConversionException ex)
            {
                Console.Error.WriteLine("The sheet export has problems:");
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (BreedDataException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static string Normalize(string s) => s.Replace("\r\n", "\n").Trim();

        private static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "HorsePersonalities.sln"))) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
