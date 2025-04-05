namespace TPP.Inputting.Inputs;

/// <summary>
/// A touchscreen input is an input in the form of <c>x,y</c>, e.g. <c>120,215</c>,
/// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
/// </summary>
public class TouchscreenInput(
    string displayedText,
    string buttonName,
    string originalText,
    uint x,
    uint y)
    : Input(displayedText, buttonName, originalText)
{
    public uint X { get; } = x;
    public uint Y { get; } = y;

    public override string ToInputString() => X + "," + Y;

    #region polymorphic equals boilerplate

    public override bool Equals(object? obj)
    {
        if (!base.Equals(obj)) return false;
        TouchscreenInput touchscreenInput = (TouchscreenInput)obj;
        return X == touchscreenInput.X && Y == touchscreenInput.Y;
    }

    public override int GetHashCode() => base.GetHashCode() | (int)X | (int)Y;

    public override bool HasSameOutcomeAs(Input? obj)
    {
        if (!base.HasSameOutcomeAs(obj)) return false;
        var touchscreenInput = (TouchscreenInput)obj!;
        return X == touchscreenInput.X && Y == touchscreenInput.Y;
    }

    public override int GetEffectiveHashCode() => base.GetEffectiveHashCode() | (int)X | (int)Y;

    #endregion
}
