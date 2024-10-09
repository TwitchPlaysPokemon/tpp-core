using System.Collections.Generic;
using NodaTime;

namespace TPP.Common.Utils;

/// A counter whose increments decay after a fixed time.
/// Useful for counting how often something happened within the past x minutes or so.
public class TtlCount(Duration ttl, IClock clock)
{
    private readonly Queue<Instant> _queue = new();

    private void Purge()
    {
        Instant limit = clock.GetCurrentInstant() - ttl;
        while (_queue.Count > 0 && _queue.Peek() < limit) _queue.Dequeue();
    }

    public void Increment()
    {
        _queue.Enqueue(clock.GetCurrentInstant());
    }

    public int Count
    {
        get
        {
            Purge();
            return _queue.Count;
        }
    }
}
