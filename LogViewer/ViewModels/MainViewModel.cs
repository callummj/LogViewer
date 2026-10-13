using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LogViewer.Misc;
using LogViewer.Services;
using Serilog;

namespace LogViewer.ViewModels;

public class MainViewModel : ViewModelBase, IMainViewModel
{
    private const string Identifier = "MainViewModel::";
    private readonly ILogger m_log;

    private readonly IFileSelectionService m_fileSelectionService;
    private readonly IDependencyInjectionContainer m_factory;
    
    private readonly AsyncRelayCommand m_openLogFileCommand;
    
    public MainViewModel(ILogger log, IDependencyInjectionContainer factory, IFileSelectionService fileSelectionService)
    {
        m_log = log;
        m_factory = factory;
        m_fileSelectionService = fileSelectionService;
        m_openLogFileCommand = new AsyncRelayCommand(ExecuteOpenLogFileCommand, CanExecuteOpenLogFileCommand);
    }

    public override async Task InitializeAsync(CancellationToken cancel)
    {
        LogDashboardViewModel = await m_factory.ResolveViewModelAsync<ILogDashboardViewModel>(cancel);


        CurrentViewModel = LogDashboardViewModel;
        await base.InitializeAsync(cancel);
    }

    #region Public Properties

    public IViewModel CurrentViewModel
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    public ILogDashboardViewModel LogDashboardViewModel
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    
    #endregion
    
    #region MenuCommands

    public ICommand OpenLogFilesCommand => m_openLogFileCommand;

    #endregion

    #region Execute Menu Commands

    private bool CanExecuteOpenLogFileCommand()
    {
        return true;
    }

    private async Task ExecuteOpenLogFileCommand(CancellationToken cancel)
    {
        m_log.Debug("{0}ExecuteOpenLogFileCommand()", Identifier);

        var files = await m_fileSelectionService.SelectFilesAsync(FilePatterns.LogFilePatterns);
        foreach (var file in files)
        {
            m_log.Debug("{0} selected: {1}", Identifier, file);
            _ = LogDashboardViewModel.OpenLogAsync(file.Path, cancel);
        }
    }

    #endregion
}