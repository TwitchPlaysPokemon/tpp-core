using System.Collections.Generic;

namespace Common
{
    public static class ListExtensions
    {
        public static List<T> AddRangeReturn<T>(this List<T> value, IEnumerable<T> collection)
        {
            value.AddRange(collection);
            return value;
        }
    }
}
