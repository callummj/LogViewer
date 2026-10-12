using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Serilog;

namespace LogViewer.Services;

public class FileSelectionService : IFileSelectionService
{
    private readonly ILogger m_log;
    private const string Identifier = "FileSelectionService::";
    
    public async Task<IEnumerable<IStorageFile?>> SelectFilesAsync(IReadOnlyList<FilePickerFileType>? fileTypeFilter)
    {
        if (!(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop))
        {
            m_log.Error("{0}SelectFilesAsync() ApplicationLifetime is null!", Identifier);
            return null;
        }

        // Fall back onto the main window's storage engine safely
        var storageProvider = desktop.MainWindow?.StorageProvider;

        if (storageProvider == null)
        {            
            m_log.Error("{0}SelectFilesAsync() StorageProvider is null! System: {1}", Identifier,
                Environment.OSVersion.Platform);
            return null;
        }
        
        var files
            = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select files",
                AllowMultiple = true,
                FileTypeFilter = fileTypeFilter,
            });

        return files;
    }
}