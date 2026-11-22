using System;
using System.Collections.Generic;

namespace HorsePersonalities.Core.Logging
{
    public enum LogLevel { Debug = 0, Info = 1, Warn = 2, Error = 3 }

    /// <summary>
    /// Where the mod writes "what it did and why". The game layer writes to scripts/HorsePersonalities/log.txt;
    /// tests use <see cref="MemoryLog"/>.
    /// </summary>
    public interface ILog
    {
        void Write(LogLevel level, string category, string message);
    }

    public static class LogExtensions
    {
        public static void Debug(this ILog log, string category, string message) => log?.Write(LogLevel.Debug, category, message);
        public static void Info(this ILog log, string category, string message) => log?.Write(LogLevel.Info, category, message);
        public static void Warn(this ILog log, string category, string message) => log?.Write(LogLevel.Warn, category, message);
        public static void Error(this ILog log, string category, string message) => log?.Write(LogLevel.Error, category, message);

        public static void Error(this ILog log, string category, string message, Exception ex) =>
            log?.Write(LogLevel.Error, category, $"{message}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    /// <summary>Formats one log line. Shared so the file log and tests agree on the format.</summary>
    public static class LogFormat
    {
        public static string Line(DateTime time, LogLevel level, string category, string message) =>
            $"{time:yyyy-MM-dd HH:mm:ss.fff} [{level.ToString().ToUpperInvariant(),-5}] {category}: {message}";
    }

    public sealed class MemoryLog : ILog
    {
        public readonly List<(LogLevel Level, string Category, string Message)> Entries = new List<(LogLevel, string, string)>();

        public void Write(LogLevel level, string category, string message) => Entries.Add((level, category, message));
    }

    public sealed class NullLog : ILog
    {
        public static readonly NullLog Instance = new NullLog();
        public void Write(LogLevel level, string category, string message) { }
    }
}
