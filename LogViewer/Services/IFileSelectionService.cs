using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace LogViewer.Services;

public interface IFileSelectionService
{
    Task<IEnumerable<IStorageFile?>> SelectFilesAsync(IReadOnlyList<FilePickerFileType>? fileTypeFilter);
}