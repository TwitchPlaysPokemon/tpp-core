using System.Collections.Generic;
using System.Linq;

namespace TPP.Core.Chat
{
    /// A queue whose items are sorted by the number of occurrences of a key in the queue,
    /// so many entries with the same key get prioritized lower than others.
    public class KeyCountPrioritizedQueue<K, V> where K : notnull where V : class
    {
        private readonly List<(K, V)> _store = new();

        public int Count => _store.Count;

        public void Enqueue(K key, V value)
        {
            _store.Add((key, value));
            Sort();
        }

        private void Sort()
        {
            Dictionary<K, int> counts = _store
                .GroupBy(tuple => tuple.Item1)
                .ToDictionary(grp => grp.Key, grp => grp.Count());
            _store.Sort((kvp1, kvp2) => counts[kvp1.Item1] - counts[kvp2.Item1]);
        }

        public (K, V)? Dequeue()
        {
            if (_store.Count == 0) return null;
            (K, V) kvp = _store.First();
            _store.RemoveAt(0);
            return kvp;
        }

        public (K, V)? DequeueLast()
        {
            if (_store.Count == 0) return null;
            (K, V) kvp = _store.Last();
            _store.RemoveAt(_store.Count - 1);
            return kvp;
        }
    }
}
