using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Inputting
{
    /// <summary>
    /// An input sequence is a sequence of <see cref="InputSet"/> that are inputted in sequence.
    /// This is used e.g. in democracy mode.
    /// </summary>
    public readonly struct InputSequence : IEquatable<InputSequence>
    {
        public IImmutableList<InputSet> InputSets { get; }
        public bool Equals(InputSequence other) => InputSets.SequenceEqual(other.InputSets);

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

        public InputSequence(IEnumerable<InputSet> inputSets)
        {
            InputSets = inputSets.ToImmutableList();
        }

        public override string ToString() => $"InputSequence({string.Join(", ", InputSets)})";
    }
}
