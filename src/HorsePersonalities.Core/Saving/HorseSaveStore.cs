using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HorsePersonalities.Core.Identity;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Core.Saving
{
    public enum SaveSource { Main, Backup, Empty, Reset }

    public sealed class SaveLoadResult
    {
        public List<HorseRecord> Records { get; } = new List<HorseRecord>();
        public SaveSource Source { get; internal set; }
    }

    /// <summary>
    /// The files in scripts/HorsePersonalities (Build plan: Phase 4):
    ///   horses.json         the save
    ///   horses.backup.json  the previous good save, refreshed on every write
    /// A write goes to horses.json.tmp first and then replaces horses.json, so a crash mid-write leaves the old file.
    /// A broken horses.json is moved aside, never deleted, and the backup loads instead.
    /// Only events call <see cref="Save"/> (rule 7): never call it from the per-frame tick.
    /// </summary>
    public sealed class HorseSaveStore
    {
        private const string Log = "Save";
        private readonly string _dir;
        private readonly ILog _log;

        public HorseSaveStore(string dataDir, ILog log)
        {
            _dir = dataDir ?? throw new ArgumentNullException(nameof(dataDir));
            _log = log ?? NullLog.Instance;
        }

        public string MainFile => Path.Combine(_dir, "horses.json");
        public string BackupFile => Path.Combine(_dir, "horses.backup.json");
        private string TempFile => Path.Combine(_dir, "horses.json.tmp");

        /// <param name="reset">ResetAllHorses: move the save aside and start empty, for a new playthrough.</param>
        public SaveLoadResult Load(bool reset, DateTime now)
        {
            var result = new SaveLoadResult();
            if (reset)
            {
                if (File.Exists(MainFile))
                {
                    string aside = Aside("reset", now);
                    File.Move(MainFile, aside);
                    _log.Info(Log, $"ResetAllHorses: moved the old save to {Path.GetFileName(aside)} and started empty.");
                }
                else _log.Info(Log, "ResetAllHorses: no save to reset.");
                result.Source = SaveSource.Reset;
                return result;
            }

            if (TryRead(MainFile, result.Records, out string error))
            {
                result.Source = SaveSource.Main;
                return result;
            }

            if (error != null)
            {
                string aside = Aside("broken", now);
                try
                {
                    File.Move(MainFile, aside);
                    _log.Error(Log, $"{error} Kept it as {Path.GetFileName(aside)}; trying the backup.");
                }
                catch (IOException ex)
                {
                    _log.Error(Log, $"{error} Could not move it aside ({ex.Message}); trying the backup.");
                }
            }
            else if (File.Exists(BackupFile))
            {
                _log.Warn(Log, "horses.json is missing but a backup exists; loading the backup.");
            }

            result.Records.Clear();
            if (TryRead(BackupFile, result.Records, out string backupError))
            {
                result.Source = SaveSource.Backup;
                _log.Info(Log, $"Loaded {result.Records.Count} horses from horses.backup.json.");
                return result;
            }
            if (backupError != null) _log.Error(Log, "The backup is broken too: " + backupError + " Starting with no saved horses.");
            else if (error != null) _log.Error(Log, "No backup to load. Starting with no saved horses.");
            result.Records.Clear();
            result.Source = SaveSource.Empty;
            return result;
        }

        /// <summary>Writes all permanent records. Returns false (and logs) instead of throwing if the disk refuses.</summary>
        public bool Save(IEnumerable<HorseRecord> records)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                File.WriteAllText(TempFile, HorseSaveFormat.Write(records));
                if (File.Exists(MainFile))
                {
                    File.Copy(MainFile, BackupFile, overwrite: true);
                    File.Replace(TempFile, MainFile, null);
                }
                else File.Move(TempFile, MainFile);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _log.Error(Log, "Could not write horses.json", ex);
                return false;
            }
        }

        /// <summary>True when the file read cleanly. False with error null when it doesn't exist.</summary>
        private bool TryRead(string path, List<HorseRecord> into, out string error)
        {
            error = null;
            if (!File.Exists(path)) return false;
            try
            {
                into.AddRange(HorseSaveFormat.Read(File.ReadAllText(path), _log));
                return true;
            }
            catch (SaveFormatException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (IOException ex)
            {
                error = $"Could not read {Path.GetFileName(path)}: {ex.Message}";
                return false;
            }
        }

        private string Aside(string why, DateTime now)
        {
            string stamp = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string path = Path.Combine(_dir, $"horses.{why}-{stamp}.json");
            for (int i = 2; File.Exists(path); i++) path = Path.Combine(_dir, $"horses.{why}-{stamp}-{i}.json");
            return path;
        }
    }
}
