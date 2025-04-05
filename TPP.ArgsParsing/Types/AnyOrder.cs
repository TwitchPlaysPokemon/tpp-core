namespace TPP.ArgsParsing.Types;

/// <summary>
/// Non-generic base class used by all of the derived classes.
/// Do not use this class directly. Instead, use the derived class with the desired amount of generic parameters.
/// </summary>
public class AnyOrder
{
}

/// <summary>
/// Class encapsulating two values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public class AnyOrder<T1, T2> : AnyOrder
{
    public T1 Item1 { get; }
    public T2 Item2 { get; }

    public AnyOrder(T1 item1, T2 item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public void Deconstruct(out T1 item1, out T2 item2) =>
        (item1, item2) = (Item1, Item2);
}

/// <summary>
/// Class encapsulating three values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public class AnyOrder<T1, T2, T3> : AnyOrder
{
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }

    public AnyOrder(T1 item1, T2 item2, T3 item3)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }

    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3) =>
        (item1, item2, item3) = (Item1, Item2, Item3);
}

/// <summary>
/// Class encapsulating four values that may be parsed in any order by an <see cref="ArgsParser"/>.
/// </summary>
public class AnyOrder<T1, T2, T3, T4> : AnyOrder
{
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }

    public AnyOrder(T1 item1, T2 item2, T3 item3, T4 item4)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
    }

    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4) =>
        (item1, item2, item3, item4) = (Item1, Item2, Item3, Item4);
}
