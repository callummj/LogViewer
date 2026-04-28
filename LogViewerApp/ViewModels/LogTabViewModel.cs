using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogViewerApp.Collections;
using LogViewerApp.Models;
using LogViewerApp.Services;

namespace LogViewerApp.ViewModels;

public partial class LogTabViewModel : ObservableObject
{
    private readonly LogParser _parser = new();
    private readonly TimeSyncService _timeSync;
    private readonly SessionSyncService _sessionSync;
    private List<LogEntryViewModel> _allEntries = new();

    // Debounce + cancellation for filter
    private CancellationTokenSource? _filterCts;
    private CancellationTokenSource? _loadCts;

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
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingText = "";
    [ObservableProperty] private string _statusText = "No file loaded";

    public RangeObservableCollection<LogEntryViewModel> FilteredEntries { get; } = new();
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

    // Any filter-affecting change triggers debounced async filter
    partial void OnFilterTextChanged(string _)           => ScheduleFilter(debounceMs: 180);
    partial void OnFilterErrorChanged(bool _)            => ScheduleFilter();
    partial void OnFilterWarnChanged(bool _)             => ScheduleFilter();
    partial void OnFilterInfoChanged(bool _)             => ScheduleFilter();
    partial void OnFilterDebugChanged(bool _)            => ScheduleFilter();
    partial void OnFilterTraceChanged(bool _)            => ScheduleFilter();
    partial void OnSelectedSessionIndexChanged(int value)
    {
        ScheduleFilter();
        if (!_suppressSessionBroadcast && IsSessionSyncEnabled)
            _sessionSync.BroadcastSession(this, value);
    }

    partial void OnSelectedEntryChanged(LogEntryViewModel? value)
    {
        if (value != null && IsTimeSyncEnabled)
            _timeSync.BroadcastTime(value.Entry.Timestamp);
        ScrollToEntry?.Invoke(value);
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    public async Task LoadFileAsync(string path)
    {
        // Cancel any in-flight load
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        FilePath  = path;
        Title     = System.IO.Path.GetFileName(path);
        IsLoading = true;
        LoadingText = "Parsing log file…";
        FilteredEntries.Reset(Array.Empty<LogEntryViewModel>());
        Sessions.Clear();
        StatusText = "Loading…";

        try
        {
            var entries  = await _parser.ParseAsync(path, ct);
            ct.ThrowIfCancellationRequested();

            LoadingText = "Splitting sessions…";
            var sessions = await Task.Run(() => _parser.SplitIntoSessions(entries), ct);
            ct.ThrowIfCancellationRequested();

            AllSessions  = sessions;
            _allEntries  = entries.Select(e => new LogEntryViewModel(e)).ToList();

            foreach (var s in sessions) Sessions.Add(s);
            SelectedSessionIndex = -1;

            StatusText = $"{entries.Count:N0} entries, {sessions.Count} session(s)";
        }
        catch (OperationCanceledException) { return; }
        finally { IsLoading = false; }

        // Apply initial filter without debounce delay
        await ApplyFilterAsync(CancellationToken.None);
    }

    // Kept for callers that need a synchronous path (e.g. unit tests or quick inline use)
    public void LoadFile(string path) => _ = LoadFileAsync(path);

    // ── Filtering ─────────────────────────────────────────────────────────────

    private void ScheduleFilter(int debounceMs = 0)
    {
        _filterCts?.Cancel();
        _filterCts = new CancellationTokenSource();
        var token = _filterCts.Token;
        _ = RunFilterAfterDelay(debounceMs, token);
    }

    private async Task RunFilterAfterDelay(int delayMs, CancellationToken ct)
    {
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            ct.ThrowIfCancellationRequested();
            await ApplyFilterAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ApplyFilterAsync(CancellationToken ct)
    {
        // Snapshot mutable state before going async
        var entries    = _allEntries;
        var filter     = FilterText.ToLowerInvariant();
        var sessionIdx = SelectedSessionIndex;
        bool fErr = FilterError, fWrn = FilterWarn, fInf = FilterInfo,
             fDbg = FilterDebug, fTrc = FilterTrace;

        var results = await Task.Run(() =>
        {
            var list = new List<LogEntryViewModel>(entries.Count);
            foreach (var vm in entries)
            {
                if (ct.IsCancellationRequested) return null;
                if (sessionIdx >= 0 && vm.SessionIndex != sessionIdx) continue;
                if (!LevelAllowed(vm.Entry.Level, fErr, fWrn, fInf, fDbg, fTrc)) continue;
                if (!string.IsNullOrEmpty(filter) &&
                    !vm.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                    !vm.Logger.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                    !vm.Thread.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(vm);
            }
            return list;
        }, ct);

        if (ct.IsCancellationRequested || results == null) return;

        // Single Reset fires one CollectionChanged — DataGrid re-renders once
        FilteredEntries.Reset(results);

        StatusText = FilePath == null
            ? "No file loaded"
            : $"Showing {FilteredEntries.Count:N0} / {_allEntries.Count:N0} entries";
    }

    private static bool LevelAllowed(LogLevel level, bool err, bool wrn, bool inf, bool dbg, bool trc)
        => level switch
        {
            LogLevel.Error or LogLevel.Fatal => err,
            LogLevel.Warn  => wrn,
            LogLevel.Info  => inf,
            LogLevel.Debug => dbg,
            LogLevel.Trace => trc,
            _              => inf
        };

    // ── Sync callbacks ────────────────────────────────────────────────────────

    private void OnTimeSynced(DateTime time)
    {
        if (!IsTimeSyncEnabled || time == DateTime.MinValue) return;
        var closest = FilteredEntries
            .OrderBy(e => Math.Abs((e.Entry.Timestamp - time).Ticks))
            .FirstOrDefault();
        if (closest != null) SelectedEntry = closest;
    }

    private bool _suppressSessionBroadcast;

    private void OnSessionSynced(int sessionIndex)
    {
        if (!IsSessionSyncEnabled) return;
        _suppressSessionBroadcast = true;
        SelectedSessionIndex = sessionIndex < Sessions.Count ? sessionIndex : -1;
        _suppressSessionBroadcast = false;
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _filterCts?.Cancel();
        _timeSync.Unsubscribe(OnTimeSynced);
        _sessionSync.Unsubscribe(this);
    }
}
