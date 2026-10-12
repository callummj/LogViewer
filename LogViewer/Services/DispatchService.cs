using System;
using Avalonia;

namespace LogViewer.Services;

public static class DispatchService
{
    public static void Do(Action action)
    {
        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }
}