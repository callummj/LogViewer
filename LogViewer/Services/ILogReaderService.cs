using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.Services;

public interface ILogReaderService
{
    public delegate Task LogReadCallback(string line, CancellationToken cancel);
    
    /// <summary>
    /// Allows live reading from file on a separate thread. Caller is responsible that callback is on correct thread.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="callback"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    public Task LiveReadFileAsync(string fileName, LogReadCallback callback, CancellationToken cancel);
}