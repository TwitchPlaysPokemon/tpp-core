namespace TPP.ArgsParsing.Types
{
    public class ImplicitString
    {
        public string Name { get; internal init; }
        public static implicit operator string(ImplicitString f) => f.Name;
        public override string ToString() => Name.ToString();
        public ImplicitString(string s) => Name = s;
    }

    public class Form : ImplicitString
    {
        public Form(string s) : base(s) => Name = s;
    }
}
