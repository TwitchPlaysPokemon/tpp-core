namespace Inputting.Inputs;

public enum InputSide { Left, Right }

public class SideInput(InputSide? side, bool direct) : Input("Side", "side", side?.GetSideString() ?? "")
{
    /// Which side this input is for.
    /// Is settable because if the side is not specified explicitly within the input,
    /// choosing a side can be a non-trivial task beyond an input parser's capabilities.
    /// It seems easiest to let some post-processing just set it in-place.
    public InputSide? Side { get; set; } = side;
    public bool Direct { get; } = direct;

    public override string ToString() => $"Side({(Side.HasValue ? Side : "null")}, Direct: {Direct})";
}

public static class InputSideExtensions
{
    public static string GetSideString(this InputSide inputSide) => inputSide switch
    {
        InputSide.Left => "left",
        InputSide.Right => "right",
    };
}
