using System;
using Avalonia;

namespace LogViewer.Services;

public static class DispatchService
{
    public static void Do(Action action)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(action);
    }
}