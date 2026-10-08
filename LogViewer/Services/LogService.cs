using System;
using Serilog;

namespace LogViewer.Services;

public class LogService : ILogService, IDisposable
{
    private readonly ILogger m_log;

    private string LogLocation
        => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
           "\\LogViewer\\" + nameof(LogViewer) + ".log";

    private const string LogStartTag = "[START OF EXECUTION]";
    private const string LogEndTag = "[END OF EXECUTION]";

    public LogService()
    {
        m_log = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set baseline log level
            .WriteTo.File(LogLocation, 
                rollingInterval: RollingInterval.Day) // Save to daily files
            .CreateLogger();
        
        m_log.Information(LogStartTag);
    }

    public void Dispose()
    {
        m_log.Information(LogEndTag);
        Log.CloseAndFlush();
    }

    public void Trace(string message)
    {
        m_log.Verbose(message);
    }

    public void Debug(string message)
    {
        m_log.Debug(message);
    }

    public void Info(string message)
    {
        m_log.Information(message);
    }

    public void Warn(string message)
    {
        m_log.Warning(message);
    }

    public void Error(string message)
    {
        m_log.Error(message);
    }

    public void Fatal(string message)
    {
        m_log.Fatal(message);
    }
}