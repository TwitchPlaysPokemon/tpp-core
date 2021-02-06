namespace TPP.ArgsParsing.Types
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
    public class SignedPokeyen : ImplicitNumber // may be negative
    {
    }
    public class SignedTokens : ImplicitNumber // may be negative
    {
    }
}
