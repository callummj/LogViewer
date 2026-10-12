using System.IO;
using System.Threading;

namespace LogViewer.Services;

public class IoService : IIoService
{
  
    public Stream OpenFile(string fileName, FileMode fileMode, FileAccess access, FileShare share, CancellationToken cancel)
    {
         return new FileStream(fileName, fileMode, access, share);
    }
}