namespace Core.ArgsParsing.Types
{
    public class AnyOrder
    {
    }

    public class AnyOrder<T1, T2> : AnyOrder
    {
        public T1 Item1 { get; }
        public T2 Item2 { get; }

        public AnyOrder(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public (T1, T2) AsTuple() => (Item1, Item2);
    }

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

        public (T1, T2, T3) AsTuple() => (Item1, Item2, Item3);
    }

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

        public (T1, T2, T3, T4) AsTuple() => (Item1, Item2, Item3, Item4);
    }
}
