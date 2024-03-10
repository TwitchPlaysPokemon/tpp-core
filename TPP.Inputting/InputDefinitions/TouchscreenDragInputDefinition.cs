using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions
{
    /// <summary>
    /// A touchscreen drag input is an input in the form of <c>x1,y1>x2,y2</c>, e.g. <c>120,215>80,170</c>,
    /// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
    /// This input definition creates inputs of type <see cref="TouchscreenDragInput"/>.
    /// </summary>
    public readonly struct TouchscreenDragInputDefinition : IInputDefinition
    {
        private readonly string _touchscreenName;
        private readonly TouchscreenDimensions _dimensions;

        public TouchscreenDragInputDefinition(TouchscreenInputDefinition touchscreen)
        {
            _touchscreenName = touchscreen.Name;
            _dimensions = touchscreen.Dimensions;

            static string DigitsForDimension(uint dim) => $@"(?:[0-9]{{1,{dim.ToString().Length}}})";
            string digitsX = DigitsForDimension(_dimensions.Width);
            string digitsY = DigitsForDimension(_dimensions.Height);
            InputRegex = $@"{digitsX},{digitsY}>{digitsX},{digitsY}";
        }

        public string InputRegex { get; }

        public Input? Parse(string str)
        {
            string[] positions = str.Split(">", count: 2);
            string[] posFromSplit = positions[0].Split(",", count: 2);
            string[] posToSplit = positions[1].Split(",", count: 2);
            (uint x1, uint y1) = (uint.Parse(posFromSplit[0]), uint.Parse(posFromSplit[1]));
            (uint x2, uint y2) = (uint.Parse(posToSplit[0]), uint.Parse(posToSplit[1]));
            if (x1 >= _dimensions.Width || x2 >= _dimensions.Width || y1 >= _dimensions.Height || y2 >= _dimensions.Height)
            {
                return null;
            }
            if (_dimensions.XScale != 1)
            {
                x1 = (uint)(x1 * _dimensions.XScale);
                x2 = (uint)(x2 * _dimensions.XScale);
            }
            if (_dimensions.YScale != 1)
            {
                y1 = (uint)(y1 * _dimensions.YScale);
                y2 = (uint)(y2 * _dimensions.YScale);
            }
            x1 += _dimensions.X;
            x2 += _dimensions.X;
            y1 += _dimensions.Y;
            y2 += _dimensions.Y;
            return new TouchscreenDragInput(str, _touchscreenName, str, x1, y1, x2, y2);
        }
    }
}
