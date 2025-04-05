using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TPP.Inputting.Inputs;

namespace TPP.Inputting;

/// <summary>
/// An input set is a set of inputs being inputted simultaneously.
/// These include buttons, touch screen coordinates, waits etc.
/// </summary>
public sealed record InputSet(ImmutableList<Input> Inputs)
{
    // Need to manually define these, because lists don't implement a proper Equals and GetHashCode themselves.
    public bool Equals(InputSet? other) => other != null && Inputs.SequenceEqual(other.Inputs);
    public override int GetHashCode() => Inputs.Select(i => i.GetHashCode()).Aggregate(HashCode.Combine);

    /// <summary>
    /// Determines whether this input set is effectively equal to another input set,
    /// meaning they would cause the same action.
    /// This is done by order-agnostic comparison between all inputs,
    /// using the <see cref="Input.HasSameOutcomeAs"/> method.
    /// </summary>
    /// <param name="other">input set to check for effective equality</param>
    /// <returns>whether the other input set has the same outcome as the supplied one</returns>
    public bool HasSameOutcomeAs(InputSet other)
    {
        return new HashSet<Input>(Inputs, SameOutcomeComparer.Instance).SetEquals(other.Inputs);
    }

    public override string ToString() => string.Join("+", Inputs);
}

/// <summary>
/// An input set with additional timing info attached regarding the hold duration and the pause duration afterwards.
/// </summary>
public sealed record TimedInputSet(InputSet InputSet, float HoldDuration, float SleepDuration);

/// <summary>
/// Equality comparer for effective equality (whether two inputs would cause the same action).
/// </summary>
internal class SameOutcomeComparer : IEqualityComparer<Input>
{
    public static readonly SameOutcomeComparer Instance = new SameOutcomeComparer();
    public bool Equals(Input? x, Input? y) =>
        (x == null && y == null)
        || (x != null && y != null && x.HasSameOutcomeAs(y));
    public int GetHashCode(Input obj) => obj.GetEffectiveHashCode();
}
