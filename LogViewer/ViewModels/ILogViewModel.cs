using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LogViewer.ViewModels;

public interface ILogViewModel : IViewModel
{
    Task OpenLogAsync(Uri logLocation, CancellationToken cancel);
    public Uri FileLocation { get; }
    public string FileName { get; }
}