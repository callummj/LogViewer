using System.IO;
using System.Threading;

namespace LogViewer.Services;

public interface IIoService
{
    Stream OpenFile(string fileName, FileMode fileMode, FileAccess access, FileShare share, CancellationToken cancel);
}