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

public class DefaultInputHoldTiming(
    float minSleepDuration = 1 / 60f,
    float minPressDuration = 1 / 60f,
    float maxPressDuration = 16 / 60f,
    float maxHoldDuration = 120 / 60f)
    : IInputHoldTiming
{
    public TimedInputSet TimeInput(InputSet inputSet, float duration)
    {
        ImmutableList<Input> inputsWithoutHold = inputSet.Inputs
            .Where(input => input is not HoldInput).ToImmutableList();
        bool hold = inputsWithoutHold.Count < inputSet.Inputs.Count;

        float holdDuration = Math.Clamp(duration, minPressDuration, hold ? maxHoldDuration : maxPressDuration);
        float sleepDuration = duration - holdDuration;

        bool doesNotSleep = sleepDuration < minSleepDuration;
        bool canFitSleep = duration >= minPressDuration + minSleepDuration;
        if (!hold && doesNotSleep && canFitSleep)
        {
            holdDuration -= minSleepDuration - sleepDuration;
            sleepDuration = minSleepDuration;
        }

        return new TimedInputSet(new InputSet(inputsWithoutHold), holdDuration, sleepDuration);
    }
}
