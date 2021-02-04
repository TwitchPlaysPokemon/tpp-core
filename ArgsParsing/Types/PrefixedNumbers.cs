namespace ArgsParsing.Types
{
    public class ImplicitNumber
    {
        public int Number { get; internal init; }
        public static implicit operator int(ImplicitNumber n) => n.Number;
    }

    public class Pokeyen : ImplicitNumber
    {
    }
    public class Tokens : ImplicitNumber
    {
    }
}
