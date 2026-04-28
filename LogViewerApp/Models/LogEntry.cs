using System;

namespace LogViewerApp.Models;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal,
    Unknown
}

public class LogEntry
{
    public int LineNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Thread { get; set; } = "";
    public string Logger { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
    public string RawLine { get; set; } = "";
    public int SessionIndex { get; set; }

    public static LogLevel ParseLevel(string level) => level.ToUpperInvariant() switch
    {
        "TRACE" => LogLevel.Trace,
        "DEBUG" => LogLevel.Debug,
        "INFO"  => LogLevel.Info,
        "WARN"  => LogLevel.Warn,
        "WARNING" => LogLevel.Warn,
        "ERROR" => LogLevel.Error,
        "FATAL" => LogLevel.Fatal,
        _       => LogLevel.Unknown
    };
}
