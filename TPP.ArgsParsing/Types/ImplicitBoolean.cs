using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
