using Microsoft.Extensions.DependencyInjection;

namespace LogViewerApp.Services;

public class DiContainer : IDiContainer
{
    private readonly IServiceProvider m_serviceProvider;

    public DiContainer(IServiceProvider serviceProvider)
    {
        m_serviceProvider = serviceProvider;
    }

    public T GetService<T>()
    {
        return m_serviceProvider.GetService<T>() ?? throw new InvalidOperationException();
    }
}