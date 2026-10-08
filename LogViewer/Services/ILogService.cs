using Serilog.Core;

namespace LogViewer.Services;

public interface ILogService
{
    [MessageTemplateFormatMethod("messageTemplate")]
    void Trace(string message);
    
    [MessageTemplateFormatMethod("messageTemplate")]
    void Debug(string message);
    
    [MessageTemplateFormatMethod("messageTemplate")]
    void Info(string message);
    
    [MessageTemplateFormatMethod("messageTemplate")]
    void Warn(string message);
    
    [MessageTemplateFormatMethod("messageTemplate")]
    void Error(string message);
    
    [MessageTemplateFormatMethod("messageTemplate")]
    void Fatal(string message);
}