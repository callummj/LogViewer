using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogViewer.Services;

namespace LogViewer.ViewModels;

public class LogViewModel : ViewModelBase, ILogViewModel
{
    private readonly IIoService m_ioService;
    
    public LogViewModel(IIoService ioService)
    {
        m_ioService = ioService;
    }


    public async Task ReadFileAsync(string fileName, CancellationToken cancel)
    {
        await using var stream = m_ioService.OpenFile(fileName, FileMode.OpenOrCreate, 
            FileAccess.Read, 
            FileShare.ReadWrite, cancel);

        Memory<byte> buffer = new Memory<byte>();
        StringBuilder sb = new StringBuilder();
        int bytesRead = 0;
        do
        {
            bytesRead = await stream.ReadAsync(buffer, cancel);
            sb.Append(buffer.ToString());
        } while (bytesRead > 0);
    }

    public async Task LiveReadFileAsync(string fileName, CancellationToken cancel)
    {
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
                    DispatchService.Do(() => Logs.Add(line));
                    
                    #if DEBUG
                    Debug.WriteLine(line);           
                    #endif
                }
            }
            else
            {
                // 3. Wait a brief moment before checking the stream size again
                await Task.Delay(500, cancel);
            }
        }
    }

    public ObservableCollection<string> Logs { get; } = new();
}