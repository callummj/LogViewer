using System;
using System.Collections.Generic;

namespace LogViewerApp.Models;

public class LogSession
{
    public int Index { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<LogEntry> Entries { get; set; } = new();
    public string Label => StartTime.HasValue ? $"Session {Index + 1} — {StartTime:HH:mm:ss}" : $"Session {Index + 1}";
}
