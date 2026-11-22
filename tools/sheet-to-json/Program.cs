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
    /// Options: --breeds PATH --classes PATH --models PATH --out PATH
    /// Defaults: data/sheet-export/Breeds.csv, data/sheet-export/Classes.csv, data/horse-models.csv, data/breeds.json
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string root = FindRepoRoot();
            string breeds = Path.Combine(root, "data", "sheet-export", "Breeds.csv");
            string classes = Path.Combine(root, "data", "sheet-export", "Classes.csv");
            string models = Path.Combine(root, "data", "horse-models.csv");
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
                    case "--out": output = Next(); break;
                    case "--check": check = true; break;
                    case "-h": case "--help":
                        Console.WriteLine("sheet-to-json [--breeds Breeds.csv] [--classes Classes.csv] [--models horse-models.csv] [--out breeds.json] [--check]");
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown option {a}. Try --help.");
                        return 2;
                }
            }

            try
            {
                var json = SheetConverter.Convert(File.ReadAllText(breeds), File.ReadAllText(classes), File.ReadAllText(models));
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
