using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NodaTime;

namespace Common.Utils;

internal record TimedEntry<T>(Instant InsertedAt, T Value);

internal class TimeKeyedCollection<T> : KeyedCollection<T, TimedEntry<T>> where T : notnull
{
    protected override T GetKeyForItem(TimedEntry<T> item) => item.Value;
}

/// A set-like class, but with a time to live.
/// Items are automatically removed after the ttl and cannot be removed manually.
public class TtlSet<T>(Duration ttl, IClock clock) : IReadOnlyCollection<T>
    where T : notnull
{
    private readonly TimeKeyedCollection<T> _set = new();

    private void Purge()
    {
        Instant limit = clock.GetCurrentInstant() - ttl;
        while (_set.Count > 0 && _set.ElementAt(0).InsertedAt < limit)
            _set.RemoveAt(0);
    }

    public bool Contains(T item)
    {
        Purge();
        return _set.Contains(item);
    }

    public bool Add(T item)
    {
        Purge();
        if (Contains(item))
            return false;
        _set.Add(new TimedEntry<T>(clock.GetCurrentInstant(), item));
        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        Purge();
        return _set.Select(i => i.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count
    {
        get
        {
            Purge();
            return _set.Count;
        }
    }
}
