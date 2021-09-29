namespace TPP.Inputting.Inputs;

/// <summary>
/// A touchscreen drag input is an input in the form of <c>x1,y1>x2,y2</c>, e.g. <c>120,215>80,170</c>,
/// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
/// </summary>
public class TouchscreenDragInput : TouchscreenInput
{
    public uint X2 { get; }
    public uint Y2 { get; }

    public TouchscreenDragInput(
        string displayedText,
        string buttonName,
        string originalText,
        uint x, uint y,
        uint x2, uint y2) : base(displayedText, buttonName, originalText, x, y)
    {
        X2 = x2;
        Y2 = y2;
    }

    public override string ToString() => $"{DisplayedText}({X},{Y}>{X2},{Y2})";

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
