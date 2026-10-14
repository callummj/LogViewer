using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
    }
    
    public async Task OpenLogAsync(Uri logLocation, CancellationToken cancel)
    {
        m_log.Debug("{0}OpenLog() Opening log: {1}", Identifier, logLocation);
        
        var vm = await m_factory.ResolveViewModelAsync<ILogViewModel>(cancel);
        
        DispatchService.Do(() =>
        {
            Logs.Add(vm);
            OnPropertyChanged(nameof(Logs));
        });
        
        await vm.OpenLogAsync(logLocation, cancel);
    }

    public ObservableCollection<ILogViewModel> Logs { get; } = new();
    
    public ILogViewModel? SelectedLog { get; set; }
}