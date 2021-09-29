using TPP.Inputting.Inputs;

namespace TPP.Inputting.InputDefinitions;

/// <summary>
/// A touchscreen drag input is an input in the form of <c>x1,y1>x2,y2</c>, e.g. <c>120,215>80,170</c>,
/// with <c>x</c> and <c>y</c> being within 0 (inclusive) and the specified max width/height (exclusive).
/// This input definition creates inputs of type <see cref="TouchscreenDragInput"/>.
/// </summary>
public readonly struct TouchscreenDragInputDefinition : IInputDefinition
{
    private readonly string _touchscreenName;
    private readonly uint _width;
    private readonly uint _height;

    public TouchscreenDragInputDefinition(string touchscreenName, uint width, uint height)
    {
        _touchscreenName = touchscreenName;
        _width = width;
        _height = height;
        static string DigitsForDimension(uint dim) => $@"(?:[0-9]{{1,{dim.ToString().Length}}})";
        string digitsX = DigitsForDimension(_width);
        string digitsY = DigitsForDimension(_height);
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
        if (x1 >= _width || x2 >= _width || y1 >= _height || y2 >= _height)
        {
            return null;
        }
        return new TouchscreenDragInput(str, _touchscreenName, str, x1, y1, x2, y2);
    }
}
