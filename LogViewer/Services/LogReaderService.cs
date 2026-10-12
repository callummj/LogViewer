using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace LogViewer.Services;

public class LogReaderService : ILogReaderService
{
    private const string Identifier = "LogReaderService::";
    private readonly ILogger m_log;
    private readonly IIoService m_ioService;
    
    public LogReaderService(ILogger log, IIoService ioService)
    {
        m_log = log;
        m_ioService = ioService;
    }

    public async Task LiveReadFileAsync(string fileName, ILogReaderService.LogReadCallback callback, CancellationToken cancel)
    {
        m_log.Debug("{0}LiveReadFileAsync() Live reading file: {1}", Identifier, fileName);
        
        await using var stream = m_ioService.OpenFile(fileName, FileMode.OpenOrCreate, 
            FileAccess.Read, 
            FileShare.ReadWrite, cancel);
        
        using var reader = new StreamReader(stream);
        
        
        while (!cancel.IsCancellationRequested)
        {
            // 2. Check if new data has arrived
            if (stream.Position < stream.Length)
            {
                while (await reader.ReadLineAsync(cancel) is { } line)
                {
                    await callback(line, cancel);
                }
            }
            else
            {
                // 3. Wait a brief moment before checking the stream size again
                await Task.Delay(500, cancel);
            }
        }
    }
}