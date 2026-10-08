using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.ViewModels;

public interface IViewModel
{
    Task InitializeAsync(CancellationToken cancel);
}