using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogViewerApp.Services;

public class PersistedTab
{
    public string FilePath { get; set; } = "";
    public string FilterText { get; set; } = "";
    public bool FilterError { get; set; } = true;
    public bool FilterWarn  { get; set; } = true;
    public bool FilterInfo  { get; set; } = true;
    public bool FilterDebug { get; set; } = true;
    public bool FilterTrace { get; set; } = true;
    public int  SelectedSessionIndex { get; set; } = -1;
    public string HighlightText { get; set; } = "";
    public bool IsTimeSyncEnabled    { get; set; }
    public bool IsSessionSyncEnabled { get; set; }
    public bool IsPinned             { get; set; }
}

public class PersistedSession
{
    public List<PersistedTab> Tabs { get; set; } = new();
    public int ActiveTabIndex { get; set; }
    public bool IsTimeSyncGlobal    { get; set; }
    public bool IsSessionSyncGlobal { get; set; }
}

public class SessionPersistenceService
{
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LogViewerApp", "session.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public void Save(PersistedSession session)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath, JsonSerializer.Serialize(session, JsonOpts));
        }
        catch { /* non-fatal */ }
    }

    public PersistedSession? Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            var json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<PersistedSession>(json);
        }
        catch { return null; }
    }
}
