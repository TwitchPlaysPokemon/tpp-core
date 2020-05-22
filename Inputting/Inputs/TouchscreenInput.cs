namespace Inputting.Inputs
{
    /// <summary>
    /// A touchscreen input is an input in the form of <c>x,y</c>, e.g. <c>120,215</c>,
    /// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
    /// </summary>
    public class TouchscreenInput : Input
    {
        public int X { get; }
        public int Y { get; }

        public TouchscreenInput(
            string displayedText,
            string buttonName,
            string originalText,
            int x, int y) : base(displayedText, buttonName, originalText)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"{DisplayedText}({X},{Y})";

        #region polymorphic equals boilerplate

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj)) return false;
            var touchscreenInput = (TouchscreenInput) obj!;
            return X == touchscreenInput.X && Y == touchscreenInput.Y;
        }

        public override int GetHashCode() => base.GetHashCode() | X | Y;

        public override bool EqualsEffectively(Input? obj)
        {
            if (!base.EqualsEffectively(obj)) return false;
            var touchscreenInput = (TouchscreenInput) obj!;
            return X == touchscreenInput.X && Y == touchscreenInput.Y;
        }

        public override int GetEffectiveHashCode() => base.GetEffectiveHashCode() | X | Y;

        #endregion
    }
}
