using System.Collections.Generic;

namespace TPPCore.Irc
{
    public class IrcCaseInsensitiveStringEqualityComparer : EqualityComparer<string>
    {
        public override bool Equals(string x, string y)
        {
            return x.ToLowerIrc() == y.ToLowerIrc();
        }

        public override int GetHashCode(string obj)
        {
            return obj.ToLowerIrc().GetHashCode();
        }
    }
}
