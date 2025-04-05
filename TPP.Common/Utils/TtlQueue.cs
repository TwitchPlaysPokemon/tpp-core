using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace TPP.Common.Utils;

/// A queue, but with a time to live.
/// Items are automatically removed after the ttl and cannot be dequeued manually.
public class TtlQueue<T>(Duration ttl, IClock clock) : IReadOnlyCollection<T>
{
    private readonly Queue<(Instant, T)> _queue = new();

    private void Purge()
    {
        Instant limit = clock.GetCurrentInstant() - ttl;
        while (_queue.Count > 0 && _queue.Peek().Item1 < limit) _queue.Dequeue();
    }

    public void Enqueue(T item)
    {
        _queue.Enqueue((clock.GetCurrentInstant(), item));
    }

    public IEnumerator<T> GetEnumerator()
    {
        Purge();
        return _queue.Select(i => i.Item2).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count
    {
        get
        {
            Purge();
            return _queue.Count;
        }
    }
}
