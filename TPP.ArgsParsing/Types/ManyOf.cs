using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TPP.ArgsParsing.TypeParsers;

namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base class used by <see cref="ManyOf{T}"/>.
/// Do not use this class directly. Instead, use <see cref="ManyOf{T}"/>.
/// </summary>
public class ManyOf
{
}

/// <summary>
/// Class encapsulating a value that may be repeated multiple times.
/// Implicitly converts to an immutable list.
/// Using this type has some restrictions. See <see cref="ManyOfParser"/> for details.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ManyOf<T> : ManyOf
{
    public ImmutableList<T> Values { get; }
    public ManyOf(IEnumerable<object> values) => Values = values.Cast<T>().ToImmutableList();

    public static implicit operator ImmutableList<T>(ManyOf<T> manyOf) => manyOf.Values;
}
