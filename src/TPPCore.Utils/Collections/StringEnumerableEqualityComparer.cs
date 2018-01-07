using System;
using System.Collections.Generic;
using System.Linq;

namespace TPPCore.Utils.Collections
{
    /// <summary>
    /// Comparer of arrays containing strings for use as keys in a dictionary.
    /// </summary>
    public class StringEnumerableEqualityComparer<T>
        : EqualityComparer<T>
        where T:IEnumerable<string>
    {
        public override bool Equals(T x, T y)
        {
            return x.SequenceEqual(y, StringComparer.InvariantCulture);
        }

        public override int GetHashCode(T obj)
        {
            // https://stackoverflow.com/a/263416/1524507
            unchecked
            {
                int hash = 17;
                foreach (var item in obj) {
                    hash = hash * 23 + item.GetHashCode();
                }
                return hash;
            }

            // TODO: Use System.HashCode when 2.1 is released
            // See dotnet/corefx#14354
        }
    }
}
