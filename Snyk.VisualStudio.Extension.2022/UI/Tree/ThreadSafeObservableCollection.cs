using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Snyk.VisualStudio.Extension.UI.Tree;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    private readonly object _lock = new object();

    protected override void InsertItem(int index, T item)
    {
        lock (_lock)
        {
            base.InsertItem(index, item);
        }
    }

    protected override void RemoveItem(int index)
    {
        lock (_lock)
        {
            base.RemoveItem(index);
        }
    }

    protected override void ClearItems()
    {
        lock (_lock)
        {
            base.ClearItems();
        }
    }

    protected override void SetItem(int index, T item)
    {
        lock (_lock)
        {
            base.SetItem(index, item);
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        lock (_lock)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }
    }
}