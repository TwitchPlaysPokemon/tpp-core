using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions
{
    /// <summary>
    /// This input definition creates inputs of type <see cref="TouchscreenInput"/>.
    /// </summary>
    public readonly struct TouchscreenInputDefinition : IInputDefinition
    {
        private readonly string _touchscreenName;
        private readonly uint _width;
        private readonly uint _height;
        private readonly uint _xOffset;
        private readonly uint _yOffset;
        private readonly float _xScale;
        private readonly float _yScale;

        public TouchscreenInputDefinition(string touchscreenName, uint width, uint height, uint xOffset=0, uint yOffset=0, uint scaleWidth = 0, uint scaleHeight = 0)
        {
            _touchscreenName = touchscreenName;
            _width = width;
            _height = height;
            _xOffset = xOffset;
            _yOffset = yOffset;
            _xScale = scaleWidth > 0 ? (float)scaleWidth / width : 1f;
            _yScale = scaleHeight > 0 ? (float)scaleHeight / height : 1f;

            static string DigitsForDimension(uint dim) => $@"(?:[0-9]{{1,{dim.ToString().Length}}})";
            InputRegex = $@"{DigitsForDimension(_width)},{DigitsForDimension(_height)}";
        }

        public string InputRegex { get; }

        public string Name => _touchscreenName;
        public TouchscreenDimensions Dimensions => new(_xOffset, _yOffset, _width, _height, _xScale, _yScale);

        public Input? Parse(string str)
        {
            string[] split = str.Split(',', count: 2);
            (uint x, uint y) = (uint.Parse(split[0]), uint.Parse(split[1]));
            if (x >= _width || y >= _height)
            {
                return null;
            }
            if (_xScale != 1)
            {
                x = (uint)(x * _xScale);
            }
            if (_yScale != 1)
            {
                y = (uint)(y * _yScale);
            }
            x += _xOffset;
            y += _yOffset;
            return new TouchscreenInput(str, _touchscreenName, str, x, y);
        }
    }

    public readonly struct TouchscreenDimensions(uint x, uint y, uint width, uint height, float xScale, float yScale)
    {
        public readonly uint X = x;
        public readonly uint Y = y;
        public readonly uint Width = width;
        public readonly uint Height = height;
        public readonly float XScale = xScale;
        public readonly float YScale = yScale;
    }
}
