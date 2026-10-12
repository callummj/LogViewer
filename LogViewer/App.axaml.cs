using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LogViewer.Services;
using LogViewer.ViewModels;
using LogViewer.Views;
using Serilog;

namespace LogViewer;

public class App : Application
{
    private const string Identifier = "App::";
    private readonly DependencyInjectionContainer m_container = new();
    private readonly ILogger m_log;
    
    private string LogLocation
        => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
           "\\LogViewer\\" + nameof(LogViewer) + ".log";

    private const string LogStartTag = "[START OF EXECUTION]";
    private const string LogEndTag = "[END OF EXECUTION]";


    public App()
    {
        m_log = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set baseline log level
            .WriteTo.File(LogLocation, 
                rollingInterval: RollingInterval.Day) // Save to daily files
            .CreateLogger();
        
        m_log.Information(LogStartTag);
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = m_container.ResolveViewModelAsync<IMainViewModel>(new CancellationToken()).Result,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public override void RegisterServices()
    {
        m_log.Debug("{0}RegisterServices()", Identifier);
        
        // Singletons
        m_container.RegisterSingleton<ILogger>(m_log);
        m_container.RegisterSingleton<IDependencyInjectionContainer>(m_container);
        
        // ViewModels
        m_container.RegisterViewModel<IMainViewModel, MainViewModel>();
        m_container.RegisterViewModel<ILogViewModel, LogViewModel>();

        // Services
        m_container.RegisterService<IFileSelectionService, FileSelectionService>();
        m_container.RegisterService<ILogReaderService, LogReaderService>();
        m_container.RegisterService<IIoService, IoService>();

        base.RegisterServices();
    }
}