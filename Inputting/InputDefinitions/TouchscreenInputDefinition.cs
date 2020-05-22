using Inputting.Inputs;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// This input definition creates inputs of type <see cref="TouchscreenInput"/>.
    /// The resulting input's effective text will be "touch".
    /// </summary>
    public readonly struct TouchscreenInputDefinition : IInputDefinition
    {
        public const string EffectiveText = "touch";

        private readonly int _width;
        private readonly int _height;

        public TouchscreenInputDefinition(int width, int height)
        {
            _width = width;
            _height = height;
        }

        private const string Number = @"(?:0|[1-9]\d{0,3})";
        public string InputRegex => $@"{Number},{Number}";

        public Input? Parse(string str)
        {
            string[] split = str.Split(',', count: 2);
            (int x, int y) = (int.Parse(split[0]), int.Parse(split[1]));
            if (x >= _width || y >= _height)
            {
                return null;
            }
            return new TouchscreenInput(str, EffectiveText, str, x, y);
        }
    }
}
