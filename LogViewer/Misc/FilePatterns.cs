using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace LogViewer.Misc;

public static class FilePatterns
{
    public static IReadOnlyList<FilePickerFileType> LogFilePatterns
    {
        get
        {
            return
            [
                new FilePickerFileType("log")
                {
                    Patterns =
                    [
                        "*.log",
                        "*.txt"
                    ]
                }
            ];
        }
    }
}