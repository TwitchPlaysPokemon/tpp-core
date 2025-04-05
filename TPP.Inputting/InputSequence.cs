using System;
using System.Collections.Immutable;
using System.Linq;

namespace TPP.Inputting;

/// <summary>
/// An input sequence is a sequence of <see cref="InputSet"/> that are inputted in sequence.
/// This is used e.g. in democracy mode.
/// </summary>
public sealed record InputSequence(IImmutableList<InputSet> InputSets)
{
    // Need to manually define these, because lists don't implement a proper Equals and GetHashCode themselves.
    public bool Equals(InputSequence? other) => other != null && InputSets.SequenceEqual(other.InputSets);
    public override int GetHashCode() => InputSets.Select(i => i.GetHashCode()).Aggregate(HashCode.Combine);

    /// <summary>
    /// Determines whether this input sequence is effectively equal to another input sequence,
    /// meaning they would cause the same action.
    /// This is done by comparing all input sets,
    /// using the <see cref="InputSet.HasSameOutcomeAs"/> method.
    /// </summary>
    /// <param name="other">input sequence to check for effective equality</param>
    /// <returns>whether the other input sequence has the same outcome as the supplied one</returns>
    public bool HasSameOutcomeAs(InputSequence other)
    {
        if (InputSets.Count != other.InputSets.Count) return false;
        for (int i = 0; i < InputSets.Count; i++)
        {
            if (!InputSets[i].HasSameOutcomeAs(other.InputSets[i])) return false;
        }
        return true;
    }

    public override string ToString() => $"{nameof(InputSequence)}({string.Join(", ", InputSets)})";
}
