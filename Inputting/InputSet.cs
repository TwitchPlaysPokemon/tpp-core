using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Inputting.Inputs;

namespace Inputting
{
    /// <summary>
    /// An input set is a set of inputs being inputted simultaneously.
    /// These include buttons, touch screen coordinates, waits etc.
    /// </summary>
    public readonly struct InputSet : IEquatable<InputSet>
    {
        public ImmutableList<Input> Inputs { get; }
        public bool Equals(InputSet other) => Inputs.SequenceEqual(other.Inputs);

        /// <summary>
        /// Determines whether this input set is effectively equal to another input set,
        /// meaning they would cause the same action.
        /// This is done by order-agnostic comparison between all inputs,
        /// using the <see cref="Input.EqualsEffectively"/> method.
        /// </summary>
        /// <param name="other">input set to check for effective equality</param>
        /// <returns>whether the other input set is effectively equal</returns>
        public bool EqualsEffectively(InputSet other)
        {
            return new HashSet<Input>(Inputs, EffectiveInputEqualityComparer.Instance).SetEquals(other.Inputs);
        }

        public InputSet(IEnumerable<Input> inputs)
        {
            Inputs = inputs.ToImmutableList();
        }

        public override string ToString() => string.Join("+", Inputs);
    }

    /// <summary>
    /// Equality comparer for effective equality (whether two inputs would cause the same action).
    /// </summary>
    internal class EffectiveInputEqualityComparer : IEqualityComparer<Input>
    {
        public static readonly EffectiveInputEqualityComparer Instance = new EffectiveInputEqualityComparer();
        public bool Equals(Input? x, Input? y) =>
            (x == null && y == null)
            || (x != null && y != null && x.EqualsEffectively(y));
        public int GetHashCode(Input obj) => obj.GetEffectiveHashCode();
    }
}
