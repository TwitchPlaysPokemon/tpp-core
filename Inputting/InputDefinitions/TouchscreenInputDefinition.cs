namespace Inputting.InputDefinitions
{
    /// <summary>
    /// A touchscreen input is an input in the form of <c>x,y</c>, e.g. <c>120,215</c>,
    /// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
    /// The resulting input's effective text will be "touch".
    /// The touched coordinates will be passed via additional data in the form of a 2-tuple <c>(x, y)</c>.
    /// </summary>
    public struct TouchscreenInputDefinition : IInputDefinition
    {
        public const string EffectiveText = "touch";

        private readonly int _width;
        private readonly int _height;

        public TouchscreenInputDefinition(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public string InputRegex => @"\d{1,4},\d{1,4}";

        public Input? Parse(string str)
        {
            var split = str.Split(',', count: 2);
            (int x, int y) = (int.Parse(split[0]), int.Parse(split[1]));
            if (x >= _width || y >= _height)
            {
                return null;
            }
            return new Input(str, EffectiveText, str, additionalData: new TouchCoords(x, y));
        }
    }

    public struct TouchCoords
    {
        public int X { get; }
        public int Y { get; }

        public TouchCoords(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X},{Y})";
    }
}
