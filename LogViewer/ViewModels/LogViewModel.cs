using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogViewer.Services;
using Serilog;

namespace LogViewer.ViewModels;

public class LogViewModel : ViewModelBase, ILogViewModel
{
    private const string Identifier = "LogViewModel::";
    private readonly ILogger m_log;

    private readonly ILogReaderService m_logReaderService;
    
    public LogViewModel(ILogger log, ILogReaderService logReaderService)
    {
        m_log = log;
        m_logReaderService = logReaderService;
    }

    public async Task OpenLogAsync(Uri logLocation, CancellationToken cancel)
    {
        m_log.Debug("{0} OpenLogAsync()", Identifier);
        FileLocation = logLocation;
        await m_logReaderService.LiveReadFileAsync(logLocation.AbsolutePath, OnLogCallbackAsync, cancel);
    }
    
    private Task OnLogCallbackAsync(string line, CancellationToken cancel)
    {
        DispatchService.Do(() => Logs.Add(line + "\n"));
        return Task.CompletedTask;
    }

    
    public Uri FileLocation { get; private set; }

    public string FileName
    {
        get => Path.GetFileName(FileLocation.AbsolutePath);
    }
    
    public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
}