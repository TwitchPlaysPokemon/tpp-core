using System;
using System.Collections.Immutable;
using System.Linq;
using TPP.Inputting.Inputs;

namespace TPP.Inputting;

public interface IInputHoldTiming
{
    /// <summary>
    /// For an <see cref="InputSet"/> and a given duration,
    /// determines how long the button set should be held and how long of a pause there should be afterwards.
    /// Removes <see cref="HoldInput"/>s from the button set in the process.
    /// </summary>
    public TimedInputSet TimeInput(InputSet inputSet, float duration);
}

public class DefaultInputHoldTiming : IInputHoldTiming
{
    private readonly float _minSleepDuration;
    private readonly float _minPressDuration;
    private readonly float _maxPressDuration;
    private readonly float _maxHoldDuration;

    public DefaultInputHoldTiming(
        float minSleepDuration = 1 / 60f,
        float minPressDuration = 1 / 60f,
        float maxPressDuration = 16 / 60f,
        float maxHoldDuration = 120 / 60f)
    {
        _minSleepDuration = minSleepDuration;
        _minPressDuration = minPressDuration;
        _maxPressDuration = maxPressDuration;
        _maxHoldDuration = maxHoldDuration;
    }

    public TimedInputSet TimeInput(InputSet inputSet, float duration)
    {
        ImmutableList<Input> inputsWithoutHold = inputSet.Inputs
            .Where(input => input is not HoldInput).ToImmutableList();
        bool hold = inputsWithoutHold.Count < inputSet.Inputs.Count;

        float holdDuration = Math.Clamp(duration, _minPressDuration, hold ? _maxHoldDuration : _maxPressDuration);
        float sleepDuration = duration - holdDuration;

        bool doesNotSleep = sleepDuration < _minSleepDuration;
        bool canFitSleep = duration >= _minPressDuration + _minSleepDuration;
        if (!hold && doesNotSleep && canFitSleep)
        {
            holdDuration -= _minSleepDuration - sleepDuration;
            sleepDuration = _minSleepDuration;
        }

        return new TimedInputSet(new InputSet(inputsWithoutHold), holdDuration, sleepDuration);
    }
}
