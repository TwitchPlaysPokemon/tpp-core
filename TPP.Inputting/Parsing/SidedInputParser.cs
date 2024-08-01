using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Parsing;

/// <summary>
/// Parses inputs so that each input set includes a <see cref="SideInput"/> indicating which side it belongs to.
/// Note that the <see cref="SideInput"/>'s side may be null if it was not specified in the input.
/// In this case it may be desirable to assign a side in a later step, beyond the scope of the input parser.
/// </summary>
public class SidedInputParser(IInputParser delegateParser) : IInputParser
{
    private static readonly Regex LeftRegex =
        new(@"^(?:l)[:.@#]?(?<input>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RightRegex =
        new(@"^(?:r)[:.@#]?(?<input>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool AllowDirectedInputs { get; set; }

    public InputSequence? Parse(string text)
    {
        InputSide? inputSide = null;
        InputSequence? inputSequence = delegateParser.Parse(text);

        if (inputSequence == null)
        {
            // If the input is already a valid button, don't try to extract a side prefix.
            // Inputs should always have parsing priority over prefixes. For example, if the buttons "rup" and "up"
            // are actual buttons, we must parse an incoming "rup" as "rup", NOT as "right side" + "up".
            Match matchLeft = AllowDirectedInputs ? LeftRegex.Match(text) : Match.Empty;
            Match matchRight = AllowDirectedInputs ? RightRegex.Match(text) : Match.Empty;
            if (matchLeft.Success)
                (inputSide, inputSequence) = (InputSide.Left, delegateParser.Parse(matchLeft.Groups["input"].Value));
            else if (matchRight.Success)
                (inputSide, inputSequence) = (InputSide.Right, delegateParser.Parse(matchRight.Groups["input"].Value));
        }

        if (inputSequence == null) return null;
        bool direct = inputSide != null;
        var sideInput = new SideInput(inputSide, direct);
        return new InputSequence(inputSequence.InputSets
            .Select(set => new InputSet(set.Inputs.Append(sideInput).ToImmutableList())).ToImmutableList());
    }
}
