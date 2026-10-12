using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.ViewModels;

public interface ILogViewModel : IViewModel
{
    public Task ReadFileAsync(string fileName, CancellationToken cancel);
    
    public Task LiveReadFileAsync(string fileName, CancellationToken cancel);
}