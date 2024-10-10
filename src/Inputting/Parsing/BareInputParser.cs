using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Inputting.Inputs;

namespace Inputting.Parsing
{
    /// <summary>
    /// This input parser is capable of parsing raw input text into <see cref="InputSequence"/>s
    /// based on the supplied <see cref="IInputDefinition"/>s and some basic settings.
    /// This parser works context-free, meaning each input gets parsed in isolation
    /// and no validations across separate inputs are performed.
    /// </summary>
    public class BareInputParser : IInputParser
    {
        private readonly List<IInputDefinition> _inputDefinitions;
        private readonly int _maxSequenceLength;
        private readonly Regex _regex;

        /// <summary>
        /// Create a new input parser instance for the given settings
        /// </summary>
        /// <param name="inputDefinitions">input definitions that get accepted by this input parser</param>
        /// <param name="maxSetLength">maximum number of concurrent inputs per input set</param>
        /// <param name="maxSequenceLength">maximum number of input sets per input sequence</param>
        /// <param name="holdEnabled">whether "hold" (appending "-") is allowed</param>
        public BareInputParser(
            IEnumerable<IInputDefinition> inputDefinitions,
            int maxSetLength,
            int maxSequenceLength,
            bool holdEnabled)
        {
            if (maxSequenceLength > 9)
                // If this feature is desired, need to rewrite the regex a bit because it's used as a digit below
                throw new ArgumentException("maxSequenceLength must be at most 9, greater not supported yet");
            _inputDefinitions = inputDefinitions.ToList();
            _maxSequenceLength = maxSequenceLength;

            // xyzFirst and xyzRepeats are the same, but re-using group names in regexes is not really allowed,
            // and only worked before because the C# implementation is quite tolerant of non-standard behaviour.
            IEnumerable<string> inputRegexGroupsFirst = _inputDefinitions
                .Select((def, i) => $"(?<input{i}_1st>{def.InputRegex})");
            IEnumerable<string> inputRegexGroupsRepeats = _inputDefinitions
                .Select((def, i) => $"(?<input{i}_nth>{def.InputRegex})");
            string inputRegexFirst = string.Join("|", inputRegexGroupsFirst);
            string inputRegexRepeats = string.Join("|", inputRegexGroupsRepeats);
            string inputSetRegex = $"(?:{inputRegexFirst})";
            if (maxSetLength > 1)
            {
                inputSetRegex += $@"(?:\+(?:{inputRegexRepeats})){{0,{maxSetLength - 1}}}";
            }
            if (holdEnabled)
            {
                inputSetRegex += "(?<hold>-)?";
            }
            // repeat-group matches lazily '*?' to not match any touchscreen coords coming afterwards for example
            Debug.Assert(_maxSequenceLength <= 9, "the below regex only makes sense if this is a digit");
            string inputSequence = _maxSequenceLength > 1
                ? $"^(?<inputset>{inputSetRegex}(?<repeat>[1-{_maxSequenceLength}])??){{1,{_maxSequenceLength}}}$"
                : $"^(?<inputset>{inputSetRegex})$";
            _regex = new Regex(inputSequence, RegexOptions.IgnoreCase);
        }

        public InputSequence? Parse(string text)
        {
            Match match = _regex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            IOrderedEnumerable<Capture> CapturesFor(string groupName) => match
                .Groups[groupName]
                .Captures
                .OrderBy(c => c.Index);
            // Get the indexes that each input set ends at
            IEnumerable<int> inputSetEndIndexes = CapturesFor("inputset")
                .Select(c => c.Index + c.Length);
            // Get all captures as queues for easy consumption
            Dictionary<IInputDefinition, Queue<Capture>> defsToCaptureQueues = _inputDefinitions
                .Select((def, i) =>
                    (def, new Queue<Capture>(CapturesFor($"input{i}_1st").Concat(CapturesFor($"input{i}_nth")))))
                .Where(tuple => tuple.Item2.Any())
                .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
            var capturesHold = new Queue<Capture>(CapturesFor("hold"));
            var capturesRepeat = new Queue<Capture>(CapturesFor("repeat"));

            var inputSets = new List<InputSet>();
            foreach (int endIndex in inputSetEndIndexes)
            {
                var inputs = new List<Input>();
                var inputWithIndexes = new List<(int, Input)>();
                foreach ((IInputDefinition def, Queue<Capture> queue) in defsToCaptureQueues)
                {
                    while (queue.Any() && queue.First().Index < endIndex)
                    {
                        Capture capture = queue.Dequeue();
                        Input? input = def.Parse(capture.Value);
                        if (input == null)
                        {
                            return null;
                        }
                        inputWithIndexes.Add((capture.Index, input));
                    }
                }
                // preserve order
                inputs.AddRange(inputWithIndexes.OrderBy(tuple => tuple.Item1).Select(tuple => tuple.Item2));
                if (capturesHold.Any() && capturesHold.Peek().Index < endIndex)
                {
                    inputs.Add(HoldInput.Instance);
                    capturesHold.Dequeue();
                }
                int numRepeat = 1;
                if (capturesRepeat.Any() && capturesRepeat.Peek().Index < endIndex)
                {
                    numRepeat = int.Parse(capturesRepeat.Dequeue().Value);
                }
                var inputSet = new InputSet(inputs.ToImmutableList());
                inputSets.AddRange(Enumerable.Repeat(inputSet, numRepeat));
                // we need to check the length, because the regex cannot enforce the max length since the sequence may
                // have been lengthened with a specified number of repetitions for a button set.
                if (inputSets.Count > _maxSequenceLength)
                {
                    return null;
                }
            }
            return new InputSequence(inputSets.ToImmutableList());
        }
    }
}
