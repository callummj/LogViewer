using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.ViewModels;

public interface ILogViewModel : IViewModel
{
    Task MonitorLog(string logFileName, CancellationToken cancel);
}