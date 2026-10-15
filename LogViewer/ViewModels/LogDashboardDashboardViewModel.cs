using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LogViewer.Services;
using Serilog;

namespace LogViewer.ViewModels;

public class LogDashboardDashboardViewModel : ViewModelBase, ILogDashboardViewModel
{
    private const string Identifier = "LogDashboardDashboardViewModel::";
    private readonly ILogger m_log;
    
    private readonly IDependencyInjectionContainer m_factory;
    
    public LogDashboardDashboardViewModel(ILogger log, IDependencyInjectionContainer factory)
    {
        m_log = log;
        m_factory = factory;
        CloseLog = new AsyncRelayCommand<ILogViewModel>(ExecuteCloseLog);
        CloseAllLogs = new AsyncRelayCommand(ExecuteCloseAllLogs);
    }

    public override Task InitializeAsync(CancellationToken cancel)
    {
        Logs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsAnyLogsOpen)); 
        return base.InitializeAsync(cancel);
    }

    public async Task OpenLogAsync(Uri logLocation, CancellationToken cancel)
    {
        m_log.Debug("{0}OpenLog() Opening log: {1}", Identifier, logLocation);
        
        var vm = await m_factory.ResolveViewModelAsync<ILogViewModel>(cancel);
        
        DispatchService.Do(() =>
        {
            Logs.Add(vm);
            OnPropertyChanged(nameof(IsAnyLogsOpen));
        });
        
        await vm.OpenLogAsync(logLocation, cancel);
    }

    private Task ExecuteCloseLog(ILogViewModel? logToClose, CancellationToken cancellationToken)
    {
        m_log.Debug("{0}CloseLog() Closing log: {1}", Identifier, logToClose.FileName);
        cancellationToken.ThrowIfCancellationRequested();
        Logs.Remove(logToClose);
        return Task.CompletedTask;
    }
    
    private Task ExecuteCloseAllLogs(CancellationToken cancellationToken)
    {
        m_log.Debug("{0}ExecuteCloseAllLogs() Closing all logs", Identifier);
        cancellationToken.ThrowIfCancellationRequested();
        Logs.Clear();
        return Task.CompletedTask;
    }
    
    public ObservableCollection<ILogViewModel> Logs { get; } = new();
    
    public bool IsAnyLogsOpen => Logs.Count > 0;
    
    public ICommand CloseLog { get; }
    public ICommand CloseAllLogs { get; }
}