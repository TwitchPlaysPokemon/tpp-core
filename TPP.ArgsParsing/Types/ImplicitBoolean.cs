namespace TPP.ArgsParsing.Types
{
    public class ImplicitBoolean
    {
        public bool Value { get; internal init; }
        public static implicit operator bool(ImplicitBoolean b) => b.Value;
        public override string ToString() => Value.ToString();
    }

    public class Shiny : ImplicitBoolean
    {
    }
}
