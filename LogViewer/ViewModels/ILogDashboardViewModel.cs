using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogViewer.ViewModels;

public interface ILogDashboardViewModel : IViewModel
{
    Task OpenLogAsync(Uri logLocation, CancellationToken cancel);
}