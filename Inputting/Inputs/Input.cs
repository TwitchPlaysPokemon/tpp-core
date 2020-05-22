using System;

namespace Inputting.Inputs
{
    /// <summary>
    /// An input is the smallest amount of input that can be expressed.
    /// Multiple inputs may be bundled together in a <see cref="InputSet"/>.
    /// Inputs get defined and parsed by <see cref="IInputDefinition"/>s.
    /// </summary>
    public class Input
    {
        /// <summary>
        /// The input's representational display text.
        /// </summary>
        public string DisplayedText { get; }
        /// <summary>
        /// The text describing the actual input instruction that should be executed.
        /// </summary>
        public string EffectiveText { get; }
        /// <summary>
        /// The original text this input was parsed from.
        /// </summary>
        public string OriginalText { get; }

        public Input(string displayedText, string effectiveText, string originalText)
        {
            DisplayedText = displayedText;
            EffectiveText = effectiveText;
            OriginalText = originalText;
        }

        public override string ToString() => $"{DisplayedText}({EffectiveText})";

        #region polymorphic equals boilerplate

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Input) obj);
        }

        public override int GetHashCode() => HashCode.Combine(DisplayedText, EffectiveText, OriginalText);

        private bool Equals(Input other)
        {
            return DisplayedText == other.DisplayedText
                   && EffectiveText == other.EffectiveText
                   && OriginalText == other.OriginalText;
        }

        /// <summary>
        /// Determines whether this input is effectively equal to another input,
        /// meaning if the inputs would cause the same action.
        /// This is done by only comparing the functional parts of this input.
        /// </summary>
        /// <param name="obj">input to check for effective equality</param>
        /// <returns>whether the supplied input is effectively equal</returns>
        public virtual bool EqualsEffectively(Input? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && EffectiveText == obj.EffectiveText;
        }

        /// <summary>
        /// HashCode-implementation for <see cref="EqualsEffectively"/>.
        /// </summary>
        /// <returns>hashcode for the effective parts of this input</returns>
        public virtual int GetEffectiveHashCode() => HashCode.Combine(EffectiveText);

        #endregion
    }
}
