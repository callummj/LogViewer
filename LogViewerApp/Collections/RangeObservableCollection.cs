using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LogViewerApp.Collections;

/// <summary>
/// Replaces the entire collection in one Reset notification instead of firing
/// one CollectionChanged per item, which causes the DataGrid to re-render once.
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    public void Reset(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
