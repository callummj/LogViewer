using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LogViewerApp.Models;

namespace LogViewerApp.Services;

public class LogParser
{
    // Matches: 2024-01-15 10:23:45,123 [ThreadName] LEVEL Logger - Message
    private static readonly Regex StandardPattern = new(
        @"^(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2}[,\.]\d{3})\s+\[([^\]]*)\]\s+(\w+)\s+([^\s]+)\s+-\s+(.*)$",
        RegexOptions.Compiled);

    // Matches: 2024-01-15 10:23:45,123 LEVEL [Thread] Logger - Message
    private static readonly Regex AltPattern = new(
        @"^(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2}[,\.]\d{3})\s+(\w+)\s+\[([^\]]*)\]\s+([^\s]+)\s+-\s+(.*)$",
        RegexOptions.Compiled);

    private static readonly string[] SessionSplitMarkers =
        ["Application started", "Starting up", "Startup", "Initializing", "Bootstrap"];

    public Task<List<LogEntry>> ParseAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() => Parse(filePath, ct), ct);

    public List<LogEntry> Parse(string filePath, CancellationToken ct = default)
    {
        var entries = new List<LogEntry>();
        var lines = File.ReadAllLines(filePath);
        LogEntry? current = null;
        int lineNum = 0;

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            lineNum++;
            var entry = TryParseLine(line, lineNum);
            if (entry != null)
            {
                if (current != null) entries.Add(current);
                current = entry;
            }
            else if (current != null)
            {
                // Continuation line (e.g. stack trace)
                current.Exception = (current.Exception ?? "") + line + "\n";
            }
        }

        if (current != null) entries.Add(current);
        return entries;
    }

    private static LogEntry? TryParseLine(string line, int lineNum)
    {
        var m = StandardPattern.Match(line);
        if (m.Success)
        {
            return new LogEntry
            {
                LineNumber = lineNum,
                Timestamp  = ParseTimestamp(m.Groups[1].Value),
                Thread     = m.Groups[2].Value,
                Level      = LogEntry.ParseLevel(m.Groups[3].Value),
                Logger     = m.Groups[4].Value,
                Message    = m.Groups[5].Value,
                RawLine    = line
            };
        }

        m = AltPattern.Match(line);
        if (m.Success)
        {
            return new LogEntry
            {
                LineNumber = lineNum,
                Timestamp  = ParseTimestamp(m.Groups[1].Value),
                Level      = LogEntry.ParseLevel(m.Groups[2].Value),
                Thread     = m.Groups[3].Value,
                Logger     = m.Groups[4].Value,
                Message    = m.Groups[5].Value,
                RawLine    = line
            };
        }

        return null;
    }

    private static DateTime ParseTimestamp(string s)
    {
        s = s.Replace(',', '.');
        if (DateTime.TryParse(s, out var dt)) return dt;
        return DateTime.MinValue;
    }

    public List<LogSession> SplitIntoSessions(List<LogEntry> entries)
    {
        var sessions = new List<LogSession>();
        LogSession? current = null;

        foreach (var entry in entries)
        {
            bool isMarker = IsSessionStart(entry);
            if (current == null || isMarker)
            {
                current = new LogSession { Index = sessions.Count, StartTime = entry.Timestamp };
                sessions.Add(current);
            }
            entry.SessionIndex = current.Index;
            current.Entries.Add(entry);
            current.EndTime = entry.Timestamp;
        }

        if (sessions.Count == 0)
        {
            var s = new LogSession { Index = 0 };
            foreach (var e in entries) { e.SessionIndex = 0; s.Entries.Add(e); }
            if (s.Entries.Count > 0) { s.StartTime = s.Entries[0].Timestamp; s.EndTime = s.Entries[^1].Timestamp; }
            sessions.Add(s);
        }

        return sessions;
    }

    private static bool IsSessionStart(LogEntry entry)
    {
        foreach (var marker in SessionSplitMarkers)
            if (entry.Message.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
