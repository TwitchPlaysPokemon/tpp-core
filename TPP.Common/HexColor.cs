using System;
using System.Text.RegularExpressions;

namespace TPP.Common;

/// <summary>
/// Wrapper around a color in hexadecimal string notation, e.g. <c>#FF0000</c> for pure red.
/// </summary>
public class HexColor
{
    private static readonly Regex Regex = new(@"^#[0-9a-f]{6}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string StringWithHash { get; }
    public string StringWithoutHash => StringWithHash[1..];

    private HexColor(string stringWithHash)
    {
        if (!Regex.IsMatch(stringWithHash))
            throw new ArgumentException("hex color string must match pattern " + Regex);
        StringWithHash = stringWithHash;
    }

    public static HexColor FromWithHash(string stringWithHash) => new(stringWithHash);
    public static HexColor FromWithoutHash(string stringWithoutHash) => new('#' + stringWithoutHash);

    public override string ToString() => StringWithHash;
}
