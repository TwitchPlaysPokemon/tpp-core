using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Parsing
{
    /// <summary>
    /// Parses inputs so that each input set includes a <see cref="SideInput"/> indicating which side it belongs to.
    /// Note that the <see cref="SideInput"/>'s side may be null if it was not specified in the input.
    /// In this case it may be desirable to assign a side in a later step, beyond the scope of the input parser.
    /// </summary>
    public class SidedInputParser : IInputParser
    {
        private static readonly Regex LeftRegex =
            new(@"^(?:l)[:.@#]?(?<input>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RightRegex =
            new(@"^(?:r)[:.@#]?(?<input>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IInputParser _delegateParser;

        public bool AllowDirectedInputs { get; set; }

        public SidedInputParser(IInputParser delegateParser)
        {
            _delegateParser = delegateParser;
        }

        public InputSequence? Parse(string text)
        {
            InputSide? inputSide;
            InputSequence? inputSequence;

            Match matchLeft = AllowDirectedInputs ? LeftRegex.Match(text) : Match.Empty;
            Match matchRight = AllowDirectedInputs ? RightRegex.Match(text) : Match.Empty;
            if (matchLeft.Success)
                (inputSide, inputSequence) = (InputSide.Left, _delegateParser.Parse(matchLeft.Groups["input"].Value));
            else if (matchRight.Success)
                (inputSide, inputSequence) = (InputSide.Right, _delegateParser.Parse(matchRight.Groups["input"].Value));
            else
                (inputSide, inputSequence) = (null, _delegateParser.Parse(text));

            if (inputSequence == null) return null;
            bool direct = inputSide != null;
            var sideInput = new SideInput(inputSide, direct);
            return new InputSequence(inputSequence.InputSets
                .Select(set => new InputSet(set.Inputs.Append(sideInput).ToImmutableList())).ToImmutableList());
        }
    }
}
