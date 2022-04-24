using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TPP.Inputting;

public static class LinqExtensions
{
    private sealed class Group<T> : IGrouping<T, T>
    {
        public T Elem { get; }
        public List<T> Members { get; }
        public Group(T elem, List<T> members)
        {
            Elem = elem;
            Members = members;
        }

        public T Key => Elem;
        public IEnumerator<T> GetEnumerator() => Members.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Members).GetEnumerator();
    }

    /// <summary>
    /// Inspired by <a href="https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/GroupAdjacent.cs">MoreLINQ's GroupAdjacent</a>.
    /// Might want to replace with proper dependency on `MoreLINQ` once more than this is needed.
    /// </summary>
    public static IEnumerable<IGrouping<T, T>> GroupAdjacent<T>(
        this IEnumerable<T> source,
        Func<T, T, bool> equalityComparer)
    {
        Group<T>? group = null;
        foreach (T element in source)
        {
            if (group != null)
            {
                if (equalityComparer(group.Elem, element))
                {
                    group.Members.Add(element);
                    continue;
                }
                else
                    yield return group;
            }
            group = new Group<T>(element, new List<T> { element });
        }
        if (group != null)
            yield return group;
    }
}
