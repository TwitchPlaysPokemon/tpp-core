namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base class used by all of the derived classes.
/// Do not use this class directly. Instead, use the derived class with the desired amount of generic parameters.
/// </summary>
public class OneOf
{
}

/// <summary>
/// Class encapsulating two optional values of which only one may ever be present.
/// </summary>
public class OneOf<T1, T2> : OneOf
{
    public Optional<T1> Item1 { get; }
    public Optional<T2> Item2 { get; }

    public OneOf(Optional<T1> item1, Optional<T2> item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public (Optional<T1>, Optional<T2>) AsTuple() => (Item1, Item2);
}

/// <summary>
/// Class encapsulating three optional values of which only one may ever be present.
/// </summary>
public class OneOf<T1, T2, T3> : OneOf
{
    public Optional<T1> Item1 { get; }
    public Optional<T2> Item2 { get; }
    public Optional<T3> Item3 { get; }

    public OneOf(Optional<T1> item1, Optional<T2> item2, Optional<T3> item3)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }

    public (Optional<T1>, Optional<T2>, Optional<T3>) AsTuple() => (Item1, Item2, Item3);
}

/// <summary>
/// Class encapsulating four optional values of which only one may ever be present.
/// </summary>
public class OneOf<T1, T2, T3, T4> : OneOf
{
    public Optional<T1> Item1 { get; }
    public Optional<T2> Item2 { get; }
    public Optional<T3> Item3 { get; }
    public Optional<T4> Item4 { get; }

    public OneOf(Optional<T1> item1, Optional<T2> item2, Optional<T3> item3, Optional<T4> item4)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
    }

    public (Optional<T1>, Optional<T2>, Optional<T3>, Optional<T4>) AsTuple() => (Item1, Item2, Item3, Item4);
}
