using System.Collections.Concurrent;

namespace CommonLib;

public class CommonLog
{
    public enum LogLevel
    {
        Info,
        Warn,
        Error,
        Debug,
        None,
        Raw
    }

    public enum LogType
    {
        CommandOutput,
        RawOutput,
        Other
    }
    
    public struct LogEntry
    {
        public readonly LogLevel Level;
        public readonly LogType Type;
        
        public readonly string Source;
        public readonly string Message;

        public LogEntry(LogLevel level, LogType type, string source, string message)
        {
            Type = type;
            Level = level;
            Source = source;
            Message = message;
        }
    }
    
    public static volatile ConcurrentQueue<LogEntry> Logs = new ConcurrentQueue<LogEntry>();
    public static volatile bool IsDebugEnabled = false;

    public static void Info(string source, object message)
        => Create(LogLevel.Info, LogType.Other, source, message);
    
    public static void Warn(string source, object message)
        => Create(LogLevel.Warn, LogType.Other, source, message);
    
    public static void Error(string source, object message)
        => Create(LogLevel.Error, LogType.Other, source, message);
    
    public static void Debug(string source, object message)
        => Create(LogLevel.Debug, LogType.Other, source, message);
    
    public static void Raw(object message)
        => Create(LogLevel.Raw, LogType.RawOutput, null, message);
    
    public static void Create(LogLevel level, LogType type, string source, object message)
        => Logs.Enqueue(new LogEntry(level, type, source, message?.ToString() ?? string.Empty));
}