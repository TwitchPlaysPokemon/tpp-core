using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Parsing;

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
        _inputDefinitions = inputDefinitions.ToList();
        _maxSequenceLength = maxSequenceLength;

        IEnumerable<string> inputRegexGroups = _inputDefinitions
            .Select((def, i) => $@"(?<input{i}>{def.InputRegex})");
        string inputRegex = string.Join("|", inputRegexGroups);
        string inputSetRegex = $@"(?:{inputRegex})";
        if (maxSetLength > 1)
        {
            inputSetRegex += $@"(?:\+(?:{inputRegex})){{0,{maxSetLength - 1}}}";
        }
        if (holdEnabled)
        {
            inputSetRegex += @"(?<hold>-)?";
        }
        // repeat-group matches lazily '??' to not match any touchscreen coords coming afterwards for example
        string inputSequence = _maxSequenceLength > 1
            ? $@"^(?<inputset>{inputSetRegex}(?<repeat>[1-{_maxSequenceLength}])??){{1,{_maxSequenceLength}}}$"
            : $@"^(?<inputset>{inputSetRegex})$";
        _regex = new Regex(inputSequence, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public InputSequence? Parse(string text)
    {
        Match match = _regex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        // Get the indexes that each input set ends at
        IEnumerable<int> inputSetEndIndexes = match.Groups["inputset"].Captures
            .OrderBy(c => c.Index)
            .Select(c => c.Index + c.Length);
        // Get all captures as queues for easy consumption
        Dictionary<IInputDefinition, Queue<Capture>> defsToCaptureQueues = _inputDefinitions
            .Select((def, i) =>
                (def, new Queue<Capture>(match.Groups[$"input{i}"].Captures.OrderBy(c => c.Index))))
            .Where(tuple => tuple.Item2.Any())
            .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
        var capturesHold = new Queue<Capture>(match.Groups["hold"].Captures.OrderBy(c => c.Index));
        var capturesRepeat = new Queue<Capture>(match.Groups["repeat"].Captures.OrderBy(c => c.Index));

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
