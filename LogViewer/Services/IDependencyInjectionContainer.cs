using LogViewer.ViewModels;

namespace LogViewer.Services;

public interface IDependencyInjectionContainer
{
    public TViewModel ResolveViewModel<TViewModel>() where TViewModel : IViewModel;
}