using System;
using System.Threading;
using LogViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LogViewer.Services;

public class DependencyInjectionContainer
{
    private IServiceProvider m_serviceProvider;
    private readonly IServiceCollection m_serviceCollection;
    private readonly Lock m_lock = new Lock();

    public DependencyInjectionContainer()
    {
        m_serviceCollection = new ServiceCollection();
        m_serviceProvider = m_serviceCollection.BuildServiceProvider();
    }
    
    public void RegisterService<TService, TImplementation>()
        where TService : class where TImplementation : class, TService
    {
        lock (m_lock)
        {
            m_serviceCollection.AddScoped<TService, TImplementation>();
            m_serviceProvider = m_serviceCollection.BuildServiceProvider();
        }
    }

    public void RegisterSingleton<T>(T instance) where T : class
    {
        lock (m_lock)
        {
            m_serviceCollection.AddSingleton<T>(instance);
            m_serviceProvider = m_serviceCollection.BuildServiceProvider();
        }
    }
    
    public void RegisterViewModel<TViewModel, TImplementation>()
        where TViewModel : class where TImplementation : class, TViewModel, IViewModel
    {
        lock (m_lock)
        {
            m_serviceCollection.AddScoped<TViewModel, TImplementation>();
            m_serviceProvider = m_serviceCollection.BuildServiceProvider();
        }
    }

    public T ResolveViewModel<T>()
    {
        return m_serviceProvider.GetRequiredService<T>();
    }
}