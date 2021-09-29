using System;

namespace TPP.Inputting.Inputs;

/// <summary>
/// An analog input is an input that has a float value from 0 to 1 describing the intensity of the input.
/// E.g. for an analog button 0 means fully released and 1 means fully pressed,
/// while for an analog stick 0 means neutral and 1 means the maximum allowable distance from the neutral position.
/// The syntax for this is e.g. <c>prefix.5</c>,
/// with the <c>.5</c> being any 1-digit decimal between <c>.1</c> and <c>.9</c>.
/// If no decimal is being specified, e.g. just <c>prefix</c>, <c>1.0</c> is assumed.
/// </summary>
public class AnalogInput : Input
{
    public float Strength { get; }

    public AnalogInput(
        string displayedText,
        string buttonName,
        string originalText,
        float strength) : base(displayedText, buttonName, originalText)
    {
        if (strength < 0f || strength > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(strength), strength, "must be between 0 and 1");
        }
        Strength = strength;
    }

    public override string ToString() => $"{DisplayedText}({ButtonName}={Strength})";

    #region polymorphic equals boilerplate

    public override bool Equals(object? obj)
    {
        if (!base.Equals(obj)) return false;
        AnalogInput analogInput = (AnalogInput)obj;
        return Math.Abs(Math.Abs(Strength - analogInput.Strength)) < float.Epsilon;
    }

    public override int GetHashCode() => base.GetHashCode() | Strength.GetHashCode();

    public override bool HasSameOutcomeAs(Input? obj)
    {
        if (!base.HasSameOutcomeAs(obj)) return false;
        var analogInput = (AnalogInput)obj!;
        return Math.Abs(Strength - analogInput.Strength) < float.Epsilon;
    }

    public override int GetEffectiveHashCode() => base.GetEffectiveHashCode() | Strength.GetHashCode();

    #endregion
}
