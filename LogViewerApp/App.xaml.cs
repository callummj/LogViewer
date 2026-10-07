using System.IO;
using System.Windows;
using LogViewer.Lib.Config;
using LogViewerApp.Services;
using LogViewerApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LogViewerApp;

public partial class App : Application
{
    private IDiContainer m_dependencyContainer;
    private readonly ConfigReader m_configReader = new ConfigReader();
    private readonly ILogger m_log;
    private readonly Config m_config;
    
    public App()
    {
        var appDataLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configLocation = appDataLocation + @"\LogViewer\LogViewerApp.config";
        
        m_config = m_configReader.ReadConfigAsync(configLocation, CancellationToken.None).Result;
        
        m_log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: Path.Combine(m_config.LogFileLocation, $"{nameof(LogViewer)}-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 30,
                shared: true)
            .CreateLogger();
        
        m_log.Information("================[LOGGING STARTED]================");
    }

    #region Event Handlers
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        m_dependencyContainer = new DiContainer(serviceProvider);

        var mainViewModel = m_dependencyContainer.GetService<IMainViewModel>();
        mainViewModel.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        
        MainWindow = new MainWindow()
        {
            DataContext = mainViewModel,
        };
        
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        m_log.Information($"================[LOGGING EXIT WITH CODE: {e.ApplicationExitCode}]================");
        base.OnExit(e);
    }
    
    #endregion
    
    private void ConfigureServices(ServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ILogger>(m_log);
        
        // ViewModels
        serviceCollection.AddTransient<IMainViewModel, MainViewModel>();
    }
}
