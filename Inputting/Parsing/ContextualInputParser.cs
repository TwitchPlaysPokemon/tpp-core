using System.Collections.Generic;
using System.Linq;
using Inputting.InputDefinitions;

namespace Inputting.Parsing
{
    /// <summary>
    /// This input parser is capable of imposing restrictions on inputs being parsed
    /// that a context-free input parser could not reasonably implement by itself,
    /// such as rejecting conflicting buttons or enforcing multitouch rules.
    /// </summary>
    public class ContextualInputParser : IInputParser
    {
        private readonly IInputParser _baseInputParser;
        private readonly HashSet<(string, string)> _conflictingInputs;
        private readonly bool _multitouch;

        /// <summary>
        /// Create a new contextual input parser from a base input parser and the configured additional restrictions.
        /// </summary>
        /// <param name="baseInputParser">the base input parser being decorated,
        /// whose output is subject to the additional restrictions imposed by this parser.</param>
        /// <param name="conflictingInputs">a set of tuples of effective inputs
        /// that are considered to be conflicting with each other.</param>
        /// <param name="multitouch">whether to allow multitouch or not
        /// (multiple touchscreen inputs per button set)</param>
        public ContextualInputParser(
            IInputParser baseInputParser,
            IEnumerable<(string, string)> conflictingInputs,
            bool multitouch)
        {
            _baseInputParser = baseInputParser;
            _conflictingInputs = new HashSet<(string, string)>(conflictingInputs);
            _multitouch = multitouch;
        }

        public InputSequence? Parse(string text)
        {
            var baseInputSequenceNullable = _baseInputParser.Parse(text);
            if (baseInputSequenceNullable == null)
            {
                return null;
            }
            var baseInputSequence = baseInputSequenceNullable.Value;

            // check for duplicates
            foreach (var inputSet in baseInputSequence.InputSets)
            {
                // all effective inputs must be unique, independently from any eventual additional data.
                // touchscreen inputs are an exception. those get checked separately below.
                var seen = new HashSet<string>();
                var effectiveInputs =
                    from input in inputSet.Inputs
                    where input.EffectiveText != TouchscreenInputDefinition.EffectiveText
                    where input.EffectiveText != TouchscreenDragInputDefinition.EffectiveText
                    select input.EffectiveText;
                if (effectiveInputs.Any(input => !seen.Add(input)))
                {
                    return null;
                }
            }

            // check for duplicate touch screen inputs
            if (_multitouch)
            {
                // only check for touches with the exact same coordinates
                if (baseInputSequence.InputSets
                    .Any(inputSet => inputSet.Inputs
                        .Where(i =>
                            i.EffectiveText == TouchscreenInputDefinition.EffectiveText ||
                            i.EffectiveText == TouchscreenDragInputDefinition.EffectiveText)
                        .GroupBy(i => i.AdditionalData)
                        .Any(g => g.Count() > 1)))
                {
                    return null;
                }
            }
            else
            {
                if (baseInputSequence.InputSets
                    .Any(inputSet => inputSet.Inputs.Count(i =>
                        i.EffectiveText == TouchscreenInputDefinition.EffectiveText ||
                        i.EffectiveText == TouchscreenDragInputDefinition.EffectiveText) > 1))
                {
                    return null;
                }
            }

            // get all possible 2-item-combinations from each button set to check for conflicts
            var combinations =
                from inputSet in baseInputSequence.InputSets
                from s1 in inputSet.Inputs
                from s2 in inputSet.Inputs
                where s1.EffectiveText != s2.EffectiveText
                select (s1.EffectiveText, s2.EffectiveText);
            if (_conflictingInputs.Overlaps(combinations))
            {
                return null;
            }

            return baseInputSequence;
        }
    }
}
