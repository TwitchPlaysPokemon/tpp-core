using System.Collections.Immutable;
using System.Linq;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Parsing
{
    public class SidedInputParser : IInputParser
    {
        private static bool _sideFlipFlop;

        private readonly IInputParser _delegateParser;

        public SidedInputParser(IInputParser delegateParser)
        {
            _delegateParser = delegateParser;
        }

        public InputSequence? Parse(string text)
        {
            string[] parts = text.Split(':', count: 2);
            InputSide? inputSide = null;
            InputSequence? inputSequence;
            if (parts.Length == 2)
            {
                string side = parts[0].ToLowerInvariant();
                if (side is "left" or "l")
                {
                    inputSide = InputSide.Left;
                    inputSequence = _delegateParser.Parse(parts[1]);
                }
                else if (side is "right" or "r")
                {
                    inputSide = InputSide.Right;
                    inputSequence = _delegateParser.Parse(parts[1]);
                }
                else
                {
                    inputSequence = _delegateParser.Parse(text);
                }
            }
            else
            {
                inputSequence = _delegateParser.Parse(text);
            }
            if (inputSequence == null) return null;
            bool direct = inputSide != null;
            if (inputSide == null)
            {
                inputSide = _sideFlipFlop ? InputSide.Left : InputSide.Right;
                _sideFlipFlop = !_sideFlipFlop;
            }
            var sideInput = new SideInput(inputSide.Value, direct);
            return new InputSequence(inputSequence.InputSets
                .Select(set => new InputSet(set.Inputs.Append(sideInput).ToImmutableList())).ToImmutableList());
        }
    }
}
