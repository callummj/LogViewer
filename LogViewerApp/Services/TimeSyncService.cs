using System;
using System.Collections.Generic;

namespace LogViewerApp.Services;

public class TimeSyncService
{
    private readonly List<Action<DateTime>> _subscribers = new();
    private readonly List<Action<object, DateTime>> _explicitSubscribers = new();
    private DateTime _currentTime;
    private bool _isSyncing;

    public bool IsSyncing
    {
        get => _isSyncing;
        set { _isSyncing = value; if (!value) _subscribers.ForEach(s => s(DateTime.MinValue)); }
    }

    public double ToleranceSeconds { get; set; } = 1.0;

    public DateTime CurrentTime => _currentTime;

    public void Subscribe(Action<DateTime> callback) => _subscribers.Add(callback);
    public void Unsubscribe(Action<DateTime> callback) => _subscribers.Remove(callback);

    public void SubscribeExplicit(Action<object, DateTime> callback) => _explicitSubscribers.Add(callback);
    public void UnsubscribeExplicit(Action<object, DateTime> callback) => _explicitSubscribers.Remove(callback);

    public void BroadcastTime(DateTime time)
    {
        if (!_isSyncing) return;
        _currentTime = time;
        foreach (var sub in _subscribers) sub(time);
    }

    // Fires all tabs regardless of IsSyncing — for explicit right-click "Sync to Timestamp".
    public void BroadcastTimeExplicit(object source, DateTime time)
    {
        _currentTime = time;
        foreach (var sub in _explicitSubscribers) sub(source, time);
    }
}
