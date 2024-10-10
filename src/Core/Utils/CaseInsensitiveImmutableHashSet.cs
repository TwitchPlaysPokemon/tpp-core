using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Core.Utils;

/// <summary>
/// ImmutableHashSet can already be made case insensitive by providing a custom comparer,
/// but by putting this into a class we can use it in config classes for automatic deserialization.
/// </summary>
public sealed class CaseInsensitiveImmutableHashSet(IEnumerable<string> items) : IImmutableSet<string>
{
    private readonly ImmutableHashSet<string>
        _set = items.ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

    #region delegate

    public IEnumerator<string> GetEnumerator() => _set.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_set).GetEnumerator();
    public int Count => _set.Count;
    public IImmutableSet<string> Add(string value) => _set.Add(value);
    public IImmutableSet<string> Clear() => _set.Clear();
    public bool Contains(string value) => _set.Contains(value);
    public IImmutableSet<string> Except(IEnumerable<string> other) => _set.Except(other);
    public IImmutableSet<string> Intersect(IEnumerable<string> other) => _set.Intersect(other);
    public bool IsProperSubsetOf(IEnumerable<string> other) => _set.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<string> other) => _set.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<string> other) => _set.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<string> other) => _set.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<string> other) => _set.Overlaps(other);
    public IImmutableSet<string> Remove(string value) => _set.Remove(value);
    public bool SetEquals(IEnumerable<string> other) => _set.SetEquals(other);
    public IImmutableSet<string> SymmetricExcept(IEnumerable<string> other) => _set.SymmetricExcept(other);
    public bool TryGetValue(string equalValue, out string actualValue) => _set.TryGetValue(equalValue, out actualValue);
    public IImmutableSet<string> Union(IEnumerable<string> other) => _set.Union(other);

    #endregion
}
