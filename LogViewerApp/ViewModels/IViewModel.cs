using System.ComponentModel;

namespace LogViewerApp.ViewModels;

public interface IViewModel : INotifyPropertyChanged
{
    Task InitializeAsync(CancellationToken cancel);
    
    Task CloseAsync(CancellationToken cancel);
}