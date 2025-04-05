namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base record used by all of the derived records.
/// Do not use this record directly. Instead, use the derived record with the desired amount of generic parameters.
/// </summary>
public record OneOf;

/// <summary>
/// Record encapsulating two optional values of which only one may ever be present.
/// </summary>
public record OneOf<T1, T2>(Optional<T1> Item1, Optional<T2> Item2) : OneOf
{
    public (Optional<T1>, Optional<T2>) AsTuple() => (Item1, Item2);
}

/// <summary>
/// Record encapsulating three optional values of which only one may ever be present.
/// </summary>
public record OneOf<T1, T2, T3>(Optional<T1> Item1, Optional<T2> Item2, Optional<T3> Item3) : OneOf
{
    public (Optional<T1>, Optional<T2>, Optional<T3>) AsTuple() => (Item1, Item2, Item3);
}

/// <summary>
/// Record encapsulating four optional values of which only one may ever be present.
/// </summary>
public record OneOf<T1, T2, T3, T4>(Optional<T1> Item1, Optional<T2> Item2, Optional<T3> Item3, Optional<T4> Item4)
    : OneOf
{
    public (Optional<T1>, Optional<T2>, Optional<T3>, Optional<T4>) AsTuple() => (Item1, Item2, Item3, Item4);
}
