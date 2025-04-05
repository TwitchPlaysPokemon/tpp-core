using System.Collections.Immutable;
using NUnit.Framework;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Tests;

public class InputHoldTimingTest
{
    private const float Delta = 1 / 600f;

    private static readonly InputSet DummyInput =
        new(ImmutableList.Create(new Input("A", "A", "A")));

    private static readonly InputSet DummyInputHeld =
        new(ImmutableList.Create(new Input("A", "A", "A"), HoldInput.Instance));

    [Test]
    public void regular_with_spare_time_divides_normally()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(maxPressDuration: 10 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInput, 30 / 60f);
        Assert.That(holdDuration, Is.EqualTo(10 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(20 / 60f).Within(Delta));
    }

    [Test]
    public void regular_without_spare_time_deducts_from_hold()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(maxPressDuration: 10 / 60f, minSleepDuration: 1 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInput, 10 / 60f);
        Assert.That(holdDuration, Is.EqualTo(9 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(1 / 60f).Within(Delta));
    }

    [Test]
    public void regular_beyond_max_gets_cut_off()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(maxPressDuration: 20 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInput, 30 / 60f);
        Assert.That(holdDuration, Is.EqualTo(20 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(10 / 60f).Within(Delta));
    }

    [Test]
    public void held_just_holds()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(maxHoldDuration: 100 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInputHeld, 30 / 60f);
        Assert.That(holdDuration, Is.EqualTo(30 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(0).Within(Delta));
    }

    [Test]
    public void held_beyond_max_does_not_hold()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(maxHoldDuration: 20 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInputHeld, 30 / 60f);
        Assert.That(holdDuration, Is.EqualTo(20 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(10 / 60f).Within(Delta));
    }

    [Test]
    public void too_little_time_for_press_and_sleep_prefers_press()
    {
        IInputHoldTiming inputHoldTiming = new DefaultInputHoldTiming(minPressDuration: 1 / 60f, minSleepDuration: 1 / 60f);
        (_, float holdDuration, float sleepDuration) = inputHoldTiming.TimeInput(DummyInputHeld, 1 / 60f);
        Assert.That(holdDuration, Is.EqualTo(1 / 60f).Within(Delta));
        Assert.That(sleepDuration, Is.EqualTo(0).Within(Delta));
    }
}
