using System;

namespace TPP.Inputting.Inputs
{
    public enum InputSide { Left, Right }

    public class SideInput : Input
    {
        public InputSide Side { get; }
        public bool Direct { get; }

        public SideInput(InputSide side, bool direct) : base("Side", "side", side.GetSideString())
        {
            Side = side;
            Direct = direct;
        }
    }

    public static class InputSideExtensions
    {
        public static string GetSideString(this InputSide inputSide) => inputSide switch
        {
            InputSide.Left => "left",
            InputSide.Right => "right",
            _ => throw new ArgumentOutOfRangeException(nameof(inputSide), inputSide, null)
        };
    }
}
