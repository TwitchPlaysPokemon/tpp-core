using System.Collections.Generic;
using System.Linq;

namespace TPP.Common;

public static class DictionaryExtensions
{
    /// Whether the dictionaries have an equal set of keys, and for each key equal values.
    public static bool DictionaryEqual<K, V>(this IDictionary<K, V> self, IDictionary<K, V> other)
    {
        if (!self.Keys.SequenceEqual(other.Keys))
            return false;
        foreach ((K key, V value) in self)
            if (!Equals(value, other[key]))
                return false;
        return true;
    }
}
