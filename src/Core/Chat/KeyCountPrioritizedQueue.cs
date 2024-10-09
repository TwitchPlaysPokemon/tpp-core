using System.Collections.Generic;
using System.Linq;

namespace TPP.Core.Chat
{
    /// A queue whose items are sorted by the number of occurrences of a key in the queue,
    /// so many entries with the same key get prioritized lower than others.
    /// This queue is synchronized (using a lock), so it's safe to use concurrently.
    public class KeyCountPrioritizedQueue<K, V>
        where K : notnull
        where V : class
    {
        private List<(K, V)> _store = new();

        public int Count
        {
            get
            {
                lock (_store) { return _store.Count; }
            }
        }

        public void Enqueue(K key, V value)
        {
            lock (_store)
            {
                _store.Add((key, value));
                Sort();
            }
        }

        private void Sort()
        {
            lock (_store)
            {
                Dictionary<K, int> counts = _store
                    .GroupBy(tuple => tuple.Item1)
                    .ToDictionary(grp => grp.Key, grp => grp.Count());
                // using linq instead of List.Sort because the sort needs to be stable
                _store = _store.OrderBy(kvp => counts[kvp.Item1]).ToList();
            }
        }

        public (K, V)? Dequeue()
        {
            lock (_store)
            {
                if (_store.Count == 0) return null;
                (K, V) kvp = _store.First();
                _store.RemoveAt(0);
                return kvp;
            }
        }

        public (K, V)? DequeueLast()
        {
            lock (_store)
            {
                if (_store.Count == 0) return null;
                (K, V) kvp = _store.Last();
                _store.RemoveAt(_store.Count - 1);
                return kvp;
            }
        }
    }
}
