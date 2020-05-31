namespace ArgsParsing.Types
{
    /// <summary>
    /// Wrapper around a color in hexadecimal string notation, e.g. <c>FF0000</c> for pure red.
    /// The string is _not_ prefixed with a hash symbol.
    /// </summary>
    public class HexColor
    {
        public string HexColorString { get; }

        public HexColor(string hexColorString)
        {
            HexColorString = hexColorString;
        }

        public static implicit operator string(HexColor hexColor) => hexColor.HexColorString;
    }
}
