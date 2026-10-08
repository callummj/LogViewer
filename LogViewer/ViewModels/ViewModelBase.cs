using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LogViewer.ViewModels;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    public virtual Task InitializeAsync(CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}