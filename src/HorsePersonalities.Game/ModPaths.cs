using System.IO;

namespace HorsePersonalities.Game
{
    /// <summary>Where the mod's files live, next to the DLL in the game's scripts folder (Build plan: What ships).</summary>
    public sealed class ModPaths
    {
        public string ScriptsDir { get; }
        public string DataDir { get; }
        public string SettingsFile => Path.Combine(ScriptsDir, "HorsePersonalities.ini");
        public string BreedsFile => Path.Combine(DataDir, "breeds.json");
        public string LogFile => Path.Combine(DataDir, "log.txt");

        public ModPaths(string scriptsDir)
        {
            ScriptsDir = scriptsDir;
            DataDir = Path.Combine(scriptsDir, "HorsePersonalities");
        }

        public void EnsureDataDir() => Directory.CreateDirectory(DataDir);
    }
}
