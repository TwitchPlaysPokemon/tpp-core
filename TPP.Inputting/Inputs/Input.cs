using System;

namespace TPP.Inputting.Inputs;

/// <summary>
/// An input is the smallest amount of input that can be expressed.
/// Multiple inputs may be bundled together in a <see cref="InputSet"/>.
/// Inputs get defined and parsed by <see cref="IInputDefinition"/>s.
/// </summary>
public class Input(string displayedText, string buttonName, string originalText)
{
    /// <summary>
    /// The input's representational display text.
    /// </summary>
    public string DisplayedText { get; } = displayedText;
    /// <summary>
    /// The name of the input/button being triggered.
    /// Conceptually, a touchscreen is also one big button.
    /// </summary>
    public string ButtonName { get; } = buttonName;
    /// <summary>
    /// The original text this input was parsed from.
    /// </summary>
    public string OriginalText { get; } = originalText;

    public virtual string ToInputString() => ButtonName;

    public override string ToString()
    {
        string inputString = ToInputString();
        return DisplayedText == inputString
            ? DisplayedText
            : $"{DisplayedText}({inputString})";
    }

    #region polymorphic equals boilerplate

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Input)obj);
    }

    public override int GetHashCode() => HashCode.Combine(DisplayedText, ButtonName, OriginalText);

    private bool Equals(Input other)
    {
        return DisplayedText == other.DisplayedText
               && ButtonName == other.ButtonName
               && OriginalText == other.OriginalText;
    }

    /// <summary>
    /// Determines whether this input is effectively equal to another input,
    /// meaning if the inputs would cause the same action.
    /// This is done by only comparing the functional parts of this input.
    /// </summary>
    /// <param name="obj">input to check for effective equality</param>
    /// <returns>whether the supplied input has the same outcome as the supplied one</returns>
    public virtual bool HasSameOutcomeAs(Input? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && ButtonName == obj.ButtonName;
    }

    /// <summary>
    /// HashCode-implementation for <see cref="HasSameOutcomeAs"/>.
    /// </summary>
    /// <returns>hashcode for the effective parts of this input</returns>
    public virtual int GetEffectiveHashCode() => HashCode.Combine(ButtonName);

    #endregion
}
