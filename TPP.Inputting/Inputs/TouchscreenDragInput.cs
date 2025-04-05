namespace TPP.Inputting.Inputs;

/// <summary>
/// A touchscreen drag input is an input in the form of <c>x1,y1>x2,y2</c>, e.g. <c>120,215>80,170</c>,
/// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
/// </summary>
public class TouchscreenDragInput(
    string displayedText,
    string buttonName,
    string originalText,
    uint x,
    uint y,
    uint x2,
    uint y2)
    : TouchscreenInput(displayedText, buttonName, originalText, x, y)
{
    public uint X2 { get; } = x2;
    public uint Y2 { get; } = y2;

    public override string ToInputString() => $"{X},{Y}>{X2},{Y2}";

    #region polymorphic equals boilerplate

    public override bool Equals(object? obj)
    {
        if (!base.Equals(obj)) return false;
        TouchscreenDragInput touchscreenDragInput = (TouchscreenDragInput)obj;
        return X2 == touchscreenDragInput.X2 && Y2 == touchscreenDragInput.Y2;
    }

    public override int GetHashCode() => base.GetHashCode() | (int)X2 | (int)Y2;

    public override bool HasSameOutcomeAs(Input? obj)
    {
        if (!base.HasSameOutcomeAs(obj)) return false;
        var touchscreenDragInput = (TouchscreenDragInput)obj!;
        return X2 == touchscreenDragInput.X2 && Y2 == touchscreenDragInput.Y2;
    }

    public override int GetEffectiveHashCode() => base.GetEffectiveHashCode() | (int)X2 | (int)Y2;

    #endregion
}
