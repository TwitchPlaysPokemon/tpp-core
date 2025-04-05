using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Parsing;

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
        InputSequence? baseInputSequence = _baseInputParser.Parse(text);
        if (baseInputSequence == null) return null;

        IImmutableList<InputSet> inputSets = baseInputSequence.InputSets;
        bool hasWaitConflict = inputSets.Any(HasNonLoneWait);
        bool hasButtonDuplication = inputSets.Any(HasDuplicationExceptTouchscreen); // e.g. "L" and "L.3"
        bool hasEffectiveDuplication = inputSets.Any(HasInputsWithSameOutcome);
        bool hasIllegalMultitouch = !_multitouch && inputSets.Any(HasMultipleTouchscreenInputs);
        bool hasConflicts = _conflictingInputs.Overlaps(GetAllCombinations(inputSets));
        if (hasWaitConflict || hasButtonDuplication || hasEffectiveDuplication || hasIllegalMultitouch || hasConflicts)
        {
            return null;
        }

        return baseInputSequence;
    }

    private static bool HasNonLoneWait(InputSet inputSet) =>
        inputSet.Inputs.Count > 1 && inputSet.Inputs.Exists(i => i.ButtonName == "wait");

    private static bool HasDuplicationExceptTouchscreen(InputSet inputSet)
    {
        var seen = new HashSet<string>();
        return inputSet.Inputs.Where(i => i is not TouchscreenInput).Any(input =>
        {
            bool alreadyExisted = !seen.Add(input.ButtonName);
            return alreadyExisted;
        });
    }

    private static bool HasInputsWithSameOutcome(InputSet inputSet)
    {
        var seen = new HashSet<Input>(SameOutcomeComparer.Instance);
        return inputSet.Inputs.Any(input =>
        {
            bool alreadyExisted = !seen.Add(input);
            return alreadyExisted;
        });
    }

    private static bool HasMultipleTouchscreenInputs(InputSet inputSet) =>
        inputSet.Inputs.OfType<TouchscreenInput>().Count() > 1;

    private static IEnumerable<(string, string)> GetAllCombinations(IEnumerable<InputSet> inputSets) =>
        from inputSet in inputSets
        from s1 in inputSet.Inputs
        from s2 in inputSet.Inputs
        where s1.ButtonName != s2.ButtonName
        select (s1.ButtonName, s2.ButtonName);
}
