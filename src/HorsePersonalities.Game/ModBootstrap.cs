using System;
using System.IO;
using HorsePersonalities.Core.Breeds;
using HorsePersonalities.Core.Logging;
using HorsePersonalities.Core.Settings;

namespace HorsePersonalities.Game
{
    /// <summary>Loads settings and breed data at script start. Shared by the main script and DevTools.</summary>
    public sealed class ModBootstrap
    {
        public ModPaths Paths { get; private set; }
        public FileLog Log { get; private set; }
        public ModSettings Settings { get; private set; }
        public BreedCatalog Breeds { get; private set; }
        /// <summary>Why breeds.json could not be used, for the on-screen message. Null when it loaded.</summary>
        public string BreedsError { get; private set; }

        public static ModBootstrap Start(string scriptsDir, string logCategory)
        {
            var b = new ModBootstrap { Paths = new ModPaths(scriptsDir) };
            try { b.Paths.EnsureDataDir(); } catch { /* reported below when files fail */ }
            b.Log = new FileLog(b.Paths.LogFile);
            b.Log.Info(logCategory, $"Starting {typeof(ModBootstrap).Assembly.GetName().Version} from {scriptsDir}");

            string iniText = null;
            try
            {
                if (File.Exists(b.Paths.SettingsFile)) iniText = File.ReadAllText(b.Paths.SettingsFile);
                else
                {
                    File.WriteAllText(b.Paths.SettingsFile, ModSettings.DefaultIniText());
                    b.Log.Info("Settings", "Wrote a default HorsePersonalities.ini.");
                }
            }
            catch (Exception ex)
            {
                b.Log.Error("Settings", "Could not read or write HorsePersonalities.ini; using defaults", ex);
            }
            b.Settings = ModSettings.Load(iniText, b.Log);
            b.Log.MinLevel = b.Settings.LogLevel;

            try
            {
                b.Breeds = BreedCatalog.Load(File.ReadAllText(b.Paths.BreedsFile), b.Log);
                foreach (Breed breed in b.Breeds.Breeds)
                    b.Log.Debug("Breeds", $"{breed.Name}: {string.Join("+", breed.Classes)}, {breed.Models.Count} models, learning {breed.Learning}");
            }
            catch (FileNotFoundException)
            {
                b.BreedsError = "HorsePersonalities/breeds.json is missing.";
            }
            catch (DirectoryNotFoundException)
            {
                b.BreedsError = "The HorsePersonalities folder is missing from scripts.";
            }
            catch (BreedDataException ex)
            {
                b.BreedsError = "breeds.json has problems; see log.txt.";
                b.Log.Error("Breeds", ex.Message);
            }
            catch (Exception ex)
            {
                b.BreedsError = "Could not read breeds.json; see log.txt.";
                b.Log.Error("Breeds", "Could not read breeds.json", ex);
            }
            if (b.BreedsError != null) b.Log.Error("Breeds", b.BreedsError + " Horses will show as unknown.");
            return b;
        }
    }
}
