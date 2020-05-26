using Inputting.Inputs;

namespace Inputting.InputDefinitions
{
    /// <summary>
    /// A touchscreen drag input is an input in the form of <c>x1,y1>x2,y2</c>, e.g. <c>120,215>80,170</c>,
    /// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
    /// This input definition creates inputs of type <see cref="TouchscreenDragInput"/>.
    /// </summary>
    public readonly struct TouchscreenDragInputDefinition : IInputDefinition
    {
        private readonly string _touchscreenName;
        private readonly int _width;
        private readonly int _height;

        public TouchscreenDragInputDefinition(string touchscreenName, int width, int height)
        {
            _touchscreenName = touchscreenName;
            _width = width;
            _height = height;
        }

        private const string Number = @"(?:[0-9]{1,4})";
        public string InputRegex => $@"{Number},{Number}>{Number},{Number}";

        public Input? Parse(string str)
        {
            string[] positions = str.Split(">", count: 2);
            string[] posFromSplit = positions[0].Split(",", count: 2);
            string[] posToSplit = positions[1].Split(",", count: 2);
            (int x1, int y1) = (int.Parse(posFromSplit[0]), int.Parse(posFromSplit[1]));
            (int x2, int y2) = (int.Parse(posToSplit[0]), int.Parse(posToSplit[1]));
            if (x1 >= _width || x2 >= _width || y1 >= _height || y2 >= _height)
            {
                return null;
            }
            return new TouchscreenDragInput(str, _touchscreenName, str, x1, y1, x2, y2);
        }
    }
}
