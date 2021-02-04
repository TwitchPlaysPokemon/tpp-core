using System;

namespace TPP.ArgsParsing.Types
{
    /// <summary>
    /// Wrapper around a color in hexadecimal string notation, prefixed with a hash symbol,
    /// e.g. <c>#FF0000</c> for pure red.
    /// </summary>
    public class HexColor
    {
        public string HexColorString { get; }

        public HexColor(string hexColorString)
        {
            if (!hexColorString.StartsWith('#')) throw new ArgumentException("hex color string must start with '#'");
            HexColorString = hexColorString;
        }

        public static implicit operator string(HexColor hexColor) => hexColor.HexColorString;
    }
}
