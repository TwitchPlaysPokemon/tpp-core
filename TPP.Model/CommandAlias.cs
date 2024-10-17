using System;
using System.Linq;

namespace TPP.Model;

public sealed record CommandAlias(
    string Alias,
    string TargetCommand,
    string[] FixedArgs
)
{
    // Overridden to implement FixedArgs using SequenceEqual (good) instead of Equals which compares identity (bad).
    public bool Equals(CommandAlias? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Alias == other.Alias
               && TargetCommand == other.TargetCommand
               && FixedArgs.SequenceEqual(other.FixedArgs);
    }
    public override int GetHashCode() => HashCode.Combine(Alias, TargetCommand, FixedArgs);
}
