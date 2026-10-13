using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.ViewModels;

public interface ILogDashboardViewModel : IViewModel
{
    Task MonitorLog(string logFileName, CancellationToken cancel);
}