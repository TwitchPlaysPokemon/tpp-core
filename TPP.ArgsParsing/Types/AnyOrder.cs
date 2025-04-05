namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base record used by all of the derived records.
/// Do not use this record directly. Instead, use the derived record with the desired amount of generic parameters.
/// </summary>
public record AnyOrder;

/// <summary>
/// Record encapsulating two values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public record AnyOrder<T1, T2>(T1 Item1, T2 Item2) : AnyOrder
{
    public void Deconstruct(out T1 item1, out T2 item2) =>
        (item1, item2) = (Item1, Item2);
}

/// <summary>
/// Record encapsulating three values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public record AnyOrder<T1, T2, T3>(T1 Item1, T2 Item2, T3 Item3) : AnyOrder
{
    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3) =>
        (item1, item2, item3) = (Item1, Item2, Item3);
}

/// <summary>
/// Record encapsulating four values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public record AnyOrder<T1, T2, T3, T4>(T1 Item1, T2 Item2, T3 Item3, T4 Item4)
    : AnyOrder
{
    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4) =>
        (item1, item2, item3, item4) = (Item1, Item2, Item3, Item4);
}
