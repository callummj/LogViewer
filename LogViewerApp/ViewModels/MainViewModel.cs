using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogViewerApp.Services;
using Microsoft.Win32;

namespace LogViewerApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TimeSyncService _timeSync = new();
    private readonly SessionSyncService _sessionSync = new();
    private readonly Log4NetConfigService _log4NetService = new();
    private readonly SessionPersistenceService _persistence = new();

    [ObservableProperty] private LogTabViewModel? _activeTab;
    [ObservableProperty] private bool _isTimeSyncGlobal;
    [ObservableProperty] private bool _isSessionSyncGlobal;
    [ObservableProperty] private string _log4NetStatus = "No config loaded";
    [ObservableProperty] private string _log4NetConfigPath = "";

    public ObservableCollection<LogTabViewModel> Tabs { get; } = new();

    // Sorted view: pinned tabs always first
    public ICollectionView TabsView { get; }

    public MainViewModel()
    {
        TabsView = CollectionViewSource.GetDefaultView(Tabs);
        TabsView.SortDescriptions.Add(new SortDescription(nameof(LogTabViewModel.IsPinned), ListSortDirection.Descending));
        if (TabsView is ICollectionViewLiveShaping live)
        {
            live.IsLiveSorting = true;
            live.LiveSortingProperties.Add(nameof(LogTabViewModel.IsPinned));
        }
    }

    partial void OnIsTimeSyncGlobalChanged(bool value)
    {
        _timeSync.IsSyncing = value;
        foreach (var tab in Tabs) tab.IsTimeSyncEnabled = value;
    }

    partial void OnIsSessionSyncGlobalChanged(bool value)
    {
        _sessionSync.IsSyncing = value;
        foreach (var tab in Tabs) tab.IsSessionSyncEnabled = value;
    }

    // ── Tab commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void NewTab()
    {
        Tabs.Add(CreateTab());
        ActiveTab = Tabs[^1];
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Log files (*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*",
            Title  = "Open Log File"
        };
        if (dlg.ShowDialog() != true) return;
        var tab = CreateTab();
        tab.LoadFile(dlg.FileName);
        Tabs.Add(tab);
        ActiveTab = tab;
    }

    [RelayCommand]
    private void OpenFileInNewTab()
    {
        var dlg = new OpenFileDialog
        {
            Filter      = "Log files (*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*",
            Title       = "Open Log File in New Tab",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var file in dlg.FileNames)
        {
            var tab = CreateTab();
            tab.LoadFile(file);
            Tabs.Add(tab);
            ActiveTab = tab;
        }
    }

    [RelayCommand]
    private void CloseTab(LogTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null || tab.IsPinned) return;
        int idx = Tabs.IndexOf(tab);
        tab.Dispose();
        Tabs.Remove(tab);
        if (ActiveTab == tab)
            ActiveTab = Tabs.Count > 0 ? Tabs[Math.Max(0, idx - 1)] : null;
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        // Closes everything including pinned
        foreach (var tab in Tabs) tab.Dispose();
        Tabs.Clear();
        ActiveTab = null;
    }

    [RelayCommand]
    private void CloseUnpinnedTabs()
    {
        var toClose = new System.Collections.Generic.List<LogTabViewModel>();
        foreach (var tab in Tabs)
            if (!tab.IsPinned) toClose.Add(tab);

        foreach (var tab in toClose)
        {
            tab.Dispose();
            Tabs.Remove(tab);
        }

        if (ActiveTab != null && !Tabs.Contains(ActiveTab))
            ActiveTab = Tabs.Count > 0 ? Tabs[0] : null;
    }

    [RelayCommand]
    private void CloseOtherTabs(LogTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;

        var toClose = new System.Collections.Generic.List<LogTabViewModel>();
        foreach (var t in Tabs)
            if (t != tab && !t.IsPinned) toClose.Add(t);

        foreach (var t in toClose)
        {
            t.Dispose();
            Tabs.Remove(t);
        }

        ActiveTab = tab;
    }

    [RelayCommand]
    private void PinTab(LogTabViewModel? tab)
    {
        tab ??= ActiveTab;
        if (tab == null) return;
        tab.IsPinned = !tab.IsPinned;
    }

    // ── log4net commands ──────────────────────────────────────────────────────

    [RelayCommand]
    private void LoadLog4NetConfig()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "XML Config (*.xml;*.config)|*.xml;*.config|All files (*.*)|*.*",
            Title  = "Load log4net Configuration"
        };
        if (dlg.ShowDialog() != true) return;

        if (_log4NetService.LoadConfig(dlg.FileName))
        {
            Log4NetConfigPath = dlg.FileName;
            Log4NetStatus = $"Loaded: {Path.GetFileName(dlg.FileName)}";
        }
        else
        {
            Log4NetStatus = $"Error: {_log4NetService.ValidationError}";
        }
    }

    [RelayCommand]
    private void GenerateLog4NetConfig()
    {
        var dlg = new SaveFileDialog
        {
            Filter   = "XML Config (*.xml)|*.xml",
            FileName = "log4net.config",
            Title    = "Save Generated log4net Config"
        };
        if (dlg.ShowDialog() != true) return;

        var outDlg = new SaveFileDialog
        {
            Filter   = "Log files (*.log)|*.log",
            FileName = "application.log",
            Title    = "Choose Log Output File"
        };
        string logPath = outDlg.ShowDialog() == true ? outDlg.FileName : @"C:\Logs\application.log";

        var xml = _log4NetService.GenerateDefaultConfig(logPath);
        File.WriteAllText(dlg.FileName, xml);
        Log4NetStatus = $"Generated: {Path.GetFileName(dlg.FileName)}";
    }

    // ── Session persistence ───────────────────────────────────────────────────

    public void SaveSession()
    {
        var session = new PersistedSession
        {
            ActiveTabIndex      = ActiveTab != null ? Tabs.IndexOf(ActiveTab) : 0,
            IsTimeSyncGlobal    = IsTimeSyncGlobal,
            IsSessionSyncGlobal = IsSessionSyncGlobal
        };

        foreach (var tab in Tabs)
        {
            if (string.IsNullOrEmpty(tab.FilePath)) continue;
            session.Tabs.Add(new PersistedTab
            {
                FilePath             = tab.FilePath,
                FilterText           = tab.FilterText,
                FilterError          = tab.FilterError,
                FilterWarn           = tab.FilterWarn,
                FilterInfo           = tab.FilterInfo,
                FilterDebug          = tab.FilterDebug,
                FilterTrace          = tab.FilterTrace,
                SelectedSessionIndex = tab.SelectedSessionIndex,
                HighlightText        = tab.HighlightText,
                IsTimeSyncEnabled    = tab.IsTimeSyncEnabled,
                IsSessionSyncEnabled = tab.IsSessionSyncEnabled,
                IsPinned             = tab.IsPinned
            });
        }

        _persistence.Save(session);
    }

    public void RestoreSession()
    {
        var session = _persistence.Load();
        if (session == null) return;

        IsTimeSyncGlobal    = session.IsTimeSyncGlobal;
        IsSessionSyncGlobal = session.IsSessionSyncGlobal;

        foreach (var p in session.Tabs)
        {
            if (!File.Exists(p.FilePath)) continue;

            var tab = CreateTab();
            tab.LoadFile(p.FilePath);
            tab.FilterText           = p.FilterText;
            tab.FilterError          = p.FilterError;
            tab.FilterWarn           = p.FilterWarn;
            tab.FilterInfo           = p.FilterInfo;
            tab.FilterDebug          = p.FilterDebug;
            tab.FilterTrace          = p.FilterTrace;
            tab.SelectedSessionIndex = p.SelectedSessionIndex;
            tab.HighlightText        = p.HighlightText;
            tab.IsTimeSyncEnabled    = p.IsTimeSyncEnabled;
            tab.IsSessionSyncEnabled = p.IsSessionSyncEnabled;
            tab.IsPinned             = p.IsPinned;
            Tabs.Add(tab);
        }

        if (Tabs.Count > 0)
            ActiveTab = Tabs[Math.Clamp(session.ActiveTabIndex, 0, Tabs.Count - 1)];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private LogTabViewModel CreateTab() => new(_timeSync, _sessionSync)
    {
        IsTimeSyncEnabled    = IsTimeSyncGlobal,
        IsSessionSyncEnabled = IsSessionSyncGlobal
    };
}
