using System;
using System.IO;
using HorsePersonalities.Core.Logging;

namespace HorsePersonalities.Game
{
    /// <summary>
    /// Appends to scripts/HorsePersonalities/log.txt. Starts the file fresh when it passes the size cap, keeping one
    /// old copy (log.old.txt), so the log stays small over long sessions. Never throws: logging must not crash the game.
    /// </summary>
    public sealed class FileLog : ILog
    {
        private readonly string _path;
        private readonly long _maxBytes;
        private readonly object _lock = new object();
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        public FileLog(string path, long maxBytes = 1024 * 1024)
        {
            _path = path;
            _maxBytes = maxBytes;
        }

        public void Write(LogLevel level, string category, string message)
        {
            if (level < MinLevel) return;
            try
            {
                lock (_lock)
                {
                    var info = new FileInfo(_path);
                    if (info.Exists && info.Length > _maxBytes)
                    {
                        string old = Path.Combine(info.DirectoryName ?? "", Path.GetFileNameWithoutExtension(_path) + ".old.txt");
                        File.Copy(_path, old, true);
                        File.Delete(_path);
                    }
                    File.AppendAllText(_path, LogFormat.Line(DateTime.Now, level, category, message) + Environment.NewLine);
                }
            }
            catch
            {
                // A locked or read-only file must never take the game down.
            }
        }
    }
}
