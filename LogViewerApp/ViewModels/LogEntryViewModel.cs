using System.Windows.Media;
using LogViewerApp.Models;

namespace LogViewerApp.ViewModels;

public class LogEntryViewModel
{
    public LogEntry Entry { get; }

    public LogEntryViewModel(LogEntry entry) => Entry = entry;

    public string Timestamp => Entry.Timestamp != System.DateTime.MinValue
        ? Entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") : "";
    public string Level      => Entry.Level.ToString().ToUpperInvariant();
    public string LevelShort => Entry.Level switch
    {
        LogLevel.Fatal => "FTL",
        LogLevel.Error => "ERR",
        LogLevel.Warn  => "WRN",
        LogLevel.Info  => "INF",
        LogLevel.Debug => "DBG",
        LogLevel.Trace => "TRC",
        _              => "???",
    };
    public string Thread      => Entry.Thread;
    public string Logger      => Entry.Logger;
    public string LoggerShort => Entry.Logger.Contains('.')
        ? Entry.Logger[(Entry.Logger.LastIndexOf('.') + 1)..]
        : Entry.Logger;
    public string Message    => Entry.Message;
    public string? Exception => Entry.Exception;
    public int LineNumber    => Entry.LineNumber;
    public int SessionIndex  => Entry.SessionIndex;

    public Brush LevelBrush => Entry.Level switch
    {
        LogLevel.Fatal => new SolidColorBrush(Color.FromRgb(160, 0, 0)),
        LogLevel.Error => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
        LogLevel.Warn  => new SolidColorBrush(Color.FromRgb(184, 134, 11)),
        LogLevel.Info  => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
        LogLevel.Debug => new SolidColorBrush(Color.FromRgb(80, 80, 80)),
        LogLevel.Trace => new SolidColorBrush(Color.FromRgb(150, 150, 150)),
        _              => new SolidColorBrush(Colors.Black)
    };

    public Brush RowBackground => Entry.Level switch
    {
        LogLevel.Fatal => new SolidColorBrush(Color.FromArgb(40, 192, 57, 43)),
        LogLevel.Error => new SolidColorBrush(Color.FromArgb(20, 192, 57, 43)),
        LogLevel.Warn  => new SolidColorBrush(Color.FromArgb(35, 184, 134, 11)),
        _              => Brushes.Transparent
    };
}
