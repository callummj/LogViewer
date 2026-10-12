using System.Threading;
using System.Threading.Tasks;
using LogViewer.ViewModels;

namespace LogViewer.Services;

public interface IDependencyInjectionContainer
{
    public Task<TViewModel> ResolveViewModelAsync<TViewModel>(CancellationToken cancel) where TViewModel : IViewModel;
}