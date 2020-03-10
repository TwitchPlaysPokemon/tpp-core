using System;

namespace Inputting
{
    /// <summary>
    /// An input is the smallest amount of input that can be expressed.
    /// Multiple inputs may be bundled together in a <see cref="InputSet"/>.
    /// Inputs get defined and parsed by <see cref="IInputDefinition"/>s.
    /// </summary>
    public struct Input : IEquatable<Input>
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
        /// <summary>
        /// Any additional data this input might have,
        /// e.g. coords for touchscreen inputs or input intensity for analog sticks.
        /// </summary>
        public object AdditionalData { get; }

        public Input(string displayedText, string effectiveText, string originalText, object additionalData)
        {
            DisplayedText = displayedText;
            EffectiveText = effectiveText;
            OriginalText = originalText;
            AdditionalData = additionalData;
        }

        public bool Equals(Input other)
        {
            return DisplayedText.Equals(other.DisplayedText)
                   && EffectiveText.Equals(other.EffectiveText)
                   && OriginalText.Equals(other.OriginalText)
                   && AdditionalData.Equals(other.AdditionalData);
        }

        /// <summary>
        /// Determines whether this input is effectively equal to another input,
        /// meaning if the inputs would cause the same action.
        /// This is done by only comparing the functional parts of this input.
        /// </summary>
        /// <param name="other">input to check for effective equality</param>
        /// <returns>whether the supplied input is effectively equal</returns>
        public bool EqualsEffectively(Input other)
        {
            return EffectiveText.Equals(other.EffectiveText) && AdditionalData.Equals(other.AdditionalData);
        }

        /// <summary>
        /// HashCode-implementation for <see cref="EqualsEffectively"/>.
        /// </summary>
        /// <returns>hashcode for the effective parts of this input</returns>
        public int GetEffectiveHashCode()
        {
            return EffectiveText.GetHashCode() | AdditionalData.GetHashCode();
        }

        public override string ToString() => $"{DisplayedText}({EffectiveText}:{AdditionalData})";
    }
}
