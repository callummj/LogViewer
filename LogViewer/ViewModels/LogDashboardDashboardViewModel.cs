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
    
    private readonly ILogReaderService m_logReaderService;
    
    public LogDashboardDashboardViewModel(ILogger log, ILogReaderService logReaderService)
    {
        m_log = log;
        m_logReaderService = logReaderService;
    }
    
    
    public Task MonitorLog(string logFileName, CancellationToken cancel)
    {
        m_log.Debug("{0}MonitorLog() Monitoring log: {1}", Identifier, logFileName);
        return m_logReaderService.LiveReadFileAsync(logFileName, OnLogCallbackAsync, cancel);
    }

    private Task OnLogCallbackAsync(string line, CancellationToken cancel)
    {
        DispatchService.Do(() => Logs.Add(line + "\n"));
        return Task.CompletedTask;
    }

    public ObservableCollection<string> Logs { get; } = new();
}