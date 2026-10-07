using CommunityToolkit.Mvvm.ComponentModel;

namespace LogViewerApp.ViewModels;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    
    public virtual Task InitializeAsync(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }

    public virtual Task CloseAsync(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
}