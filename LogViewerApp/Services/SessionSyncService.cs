using System;
using System.Collections.Generic;

namespace LogViewerApp.Services;

public class SessionSyncService
{
    private readonly List<(object Owner, Action<int> Callback)> _subscribers = new();
    private bool _isSyncing;

    public bool IsSyncing
    {
        get => _isSyncing;
        set => _isSyncing = value;
    }

    public void Subscribe(object owner, Action<int> callback)
        => _subscribers.Add((owner, callback));

    public void Unsubscribe(object owner)
        => _subscribers.RemoveAll(s => s.Owner == owner);

    public void BroadcastSession(object source, int sessionIndex)
    {
        if (!_isSyncing) return;
        foreach (var (owner, callback) in _subscribers)
            if (owner != source) callback(sessionIndex);
    }
}
