using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace TPP.Core.Utils;

/// A queue, but with a time to live.
/// Items are automatically removed after the ttl and cannot be dequeued manually.
public class TtlQueue<T> : IReadOnlyCollection<T>
{
    private readonly Duration _ttl;
    private readonly IClock _clock;
    private readonly Queue<(Instant, T)> _queue;

    public TtlQueue(Duration ttl, IClock clock)
    {
        _ttl = ttl;
        _clock = clock;
        _queue = new Queue<(Instant, T)>();
    }

    private void Purge()
    {
        Instant limit = _clock.GetCurrentInstant() - _ttl;
        while (_queue.Count > 0 && _queue.Peek().Item1 < limit) _queue.Dequeue();
    }

    public void Enqueue(T item)
    {
        _queue.Enqueue((_clock.GetCurrentInstant(), item));
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
