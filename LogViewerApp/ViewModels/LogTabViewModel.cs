using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogViewerApp.Models;
using LogViewerApp.Services;

namespace LogViewerApp.ViewModels;

public partial class LogTabViewModel : ObservableObject
{
    private readonly LogParser _parser = new();
    private readonly TimeSyncService _timeSync;
    private readonly SessionSyncService _sessionSync;
    private List<LogEntryViewModel> _allEntries = new();
    private bool _suppressSessionBroadcast;

    [ObservableProperty] private string _title = "New Tab";
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private bool _filterError = true;
    [ObservableProperty] private bool _filterWarn  = true;
    [ObservableProperty] private bool _filterInfo  = true;
    [ObservableProperty] private bool _filterDebug = true;
    [ObservableProperty] private bool _filterTrace = true;
    [ObservableProperty] private LogEntryViewModel? _selectedEntry;
    [ObservableProperty] private int _selectedSessionIndex = -1;
    [ObservableProperty] private string _highlightText = "";
    [ObservableProperty] private bool _isTimeSyncEnabled;
    [ObservableProperty] private bool _isSessionSyncEnabled;
    [ObservableProperty] private bool _isPinned;
    [ObservableProperty] private string _statusText = "No file loaded";

    public ObservableCollection<LogEntryViewModel> FilteredEntries { get; } = new();
    public ObservableCollection<LogSession> Sessions { get; } = new();
    public List<LogSession> AllSessions { get; private set; } = new();

    public Action<LogEntryViewModel?>? ScrollToEntry;

    public ICommand ClearSessionFilterCommand => new RelayCommand(() => SelectedSessionIndex = -1);

    public LogTabViewModel(TimeSyncService timeSync, SessionSyncService sessionSync)
    {
        _timeSync = timeSync;
        _sessionSync = sessionSync;
        _timeSync.Subscribe(OnTimeSynced);
        _sessionSync.Subscribe(this, OnSessionSynced);
    }

    partial void OnFilterTextChanged(string value)   => ApplyFilter();
    partial void OnFilterErrorChanged(bool value)    => ApplyFilter();
    partial void OnFilterWarnChanged(bool value)     => ApplyFilter();
    partial void OnFilterInfoChanged(bool value)     => ApplyFilter();
    partial void OnFilterDebugChanged(bool value)    => ApplyFilter();
    partial void OnFilterTraceChanged(bool value)    => ApplyFilter();

    partial void OnSelectedSessionIndexChanged(int value)
    {
        ApplyFilter();
        if (!_suppressSessionBroadcast && IsSessionSyncEnabled)
            _sessionSync.BroadcastSession(this, value);
    }

    partial void OnSelectedEntryChanged(LogEntryViewModel? value)
    {
        if (value != null && IsTimeSyncEnabled)
            _timeSync.BroadcastTime(value.Entry.Timestamp);
        ScrollToEntry?.Invoke(value);
    }

    public void LoadFile(string path)
    {
        FilePath = path;
        Title = System.IO.Path.GetFileName(path);
        var entries = _parser.Parse(path);
        var sessions = _parser.SplitIntoSessions(entries);

        AllSessions = sessions;
        _allEntries = entries.Select(e => new LogEntryViewModel(e)).ToList();

        Sessions.Clear();
        foreach (var s in sessions) Sessions.Add(s);

        SelectedSessionIndex = -1;
        ApplyFilter();
        StatusText = $"{entries.Count:N0} entries, {sessions.Count} session(s)";
    }

    public void ApplyFilter()
    {
        FilteredEntries.Clear();
        var filter = FilterText.ToLowerInvariant();

        foreach (var vm in _allEntries)
        {
            if (SelectedSessionIndex >= 0 && vm.SessionIndex != SelectedSessionIndex) continue;
            if (!LevelAllowed(vm.Entry.Level)) continue;
            if (!string.IsNullOrEmpty(filter) &&
                !vm.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !vm.Logger.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !vm.Thread.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;

            FilteredEntries.Add(vm);
        }

        StatusText = FilePath == null
            ? "No file loaded"
            : $"Showing {FilteredEntries.Count:N0} / {_allEntries.Count:N0} entries";
    }

    private bool LevelAllowed(LogLevel level) => level switch
    {
        LogLevel.Error or LogLevel.Fatal => FilterError,
        LogLevel.Warn  => FilterWarn,
        LogLevel.Info  => FilterInfo,
        LogLevel.Debug => FilterDebug,
        LogLevel.Trace => FilterTrace,
        _              => FilterInfo
    };

    private void OnTimeSynced(DateTime time)
    {
        if (!IsTimeSyncEnabled || time == DateTime.MinValue) return;
        var closest = FilteredEntries
            .OrderBy(e => Math.Abs((e.Entry.Timestamp - time).Ticks))
            .FirstOrDefault();
        if (closest != null) SelectedEntry = closest;
    }

    private void OnSessionSynced(int sessionIndex)
    {
        if (!IsSessionSyncEnabled) return;
        _suppressSessionBroadcast = true;
        // Clamp to a valid index for this file's session count
        SelectedSessionIndex = sessionIndex < Sessions.Count ? sessionIndex : -1;
        _suppressSessionBroadcast = false;
    }

    public void Dispose()
    {
        _timeSync.Unsubscribe(OnTimeSynced);
        _sessionSync.Unsubscribe(this);
    }
}
