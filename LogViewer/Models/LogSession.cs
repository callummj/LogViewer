using System;

namespace LogViewer.Models;

public class LogSession
{
    public DateTime StartTime { get; set; } = DateTime.MinValue;
    
    public DateTime EndTime { get; set; } = DateTime.MinValue;
    
}