using Avalonia;
using System;
using System.Threading.Tasks;

namespace LogViewer;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine($"[CRITICAL] Unobserved Task Exception: {e.Exception.Message}");
            e.SetObserved(); // Prevents a total process crash if you just want to log it
            throw e.Exception; // Force a crash during debugging so you see it immediately!
        };
        
        // Catch exceptions on the main UI Dispatcher thread
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.WriteLine($"[CRITICAL] Unhandled AppDomain Exception: {e.ExceptionObject}");
        };
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}