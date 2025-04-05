using System;

namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base class used by <see cref="Optional{T}"/>.
/// Do not use this class directly. Instead, use <see cref="Optional{T}"/>.
/// </summary>
public class Optional
{
}

/// <summary>
/// Class encapsulating that may be absent during parsing and not cause a parse failure,
/// but instead result in an empty instance of this class.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Optional<T>(bool present, T value) : Optional
{
    public bool IsPresent { get; } = present;

    public T Value
    {
        get
        {
            if (!IsPresent) throw new InvalidOperationException("Cannot access the value of an empty Optional.");
            return value;
        }
    }

    public T OrElse(T fallback)
    {
        return IsPresent ? Value : fallback;
    }

    public Optional<T2> Map<T2>(Func<T, T2> mapFunc) =>
        IsPresent
            ? new Optional<T2>(true, mapFunc(Value))
            : new Optional<T2>(false, default!);

    public override string ToString()
        => IsPresent ? Value?.ToString() ?? "<null>" : "<none>";
}
