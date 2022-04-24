using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests;

public class InputRepresentationTest
{
    private static InputSet Set(params string[] inputs) =>
        new(inputs.Select(s => new Input(s, s, s)).ToImmutableList());
    private static InputSequence Seq(params InputSet[] inputSets) => new(inputSets.ToImmutableList());

    [Test]
    public void repeats_collapsing_buttons()
    {
        InputSequence seq = Seq(Set("x"), Set("a", "b"), Set("b", "a"), Set("a", "b"), Set("x"));
        Assert.AreEqual("xa+b3x", seq.ToRepresentation(collapseRepeats: true));
        Assert.AreEqual("xa+bb+aa+bx", seq.ToRepresentation(collapseRepeats: false));
    }

    [Test]
    public void repeats_collapsing_touchscreen()
    {
        InputSet TouchSet(uint x, uint y) =>
            new(ImmutableList.Create<Input>(new TouchscreenInput($"{x},{y}", "touchscreen", $"{x},{y}", x, y)));
        InputSequence seq = Seq(Set("x"), TouchSet(123, 234), TouchSet(123, 234), TouchSet(321, 432), Set("x"));
        // This is hilariously awful, but technically correct
        Assert.AreEqual("x123,2342321,432x", seq.ToRepresentation(collapseRepeats: true));
        Assert.AreEqual("x123,234123,234321,432x", seq.ToRepresentation(collapseRepeats: false));
    }

    [Test]
    public void repeats_collapsing_analog()
    {
        InputSet AnalogSet(float strength) => new(ImmutableList.Create<Input>(
            new AnalogInput($"A.{strength * 10:F0}", "A", $"a.{strength * 10:F0}", strength)));
        InputSequence seq = Seq(Set("x"), AnalogSet(.5f), AnalogSet(.5f), AnalogSet(.6f), Set("x"));
        Assert.AreEqual("xa.52a.6x", seq.ToRepresentation(collapseRepeats: true));
        Assert.AreEqual("xa.5a.5a.6x", seq.ToRepresentation(collapseRepeats: false));
    }

    [Test]
    public void represent_keeps_touch_coordinates()
    {
        IInputParser parser = InputParserBuilder.FromBare()
            .Buttons("A", "B")
            .LengthRestrictions(maxSetLength: 1, maxSequenceLength: 5)
            .Touchscreen(400, 300, true, true)
            .Build();
        InputSequence? seq = parser.Parse("a123,234b123,234>234,123a");
        Assert.That(seq?.InputSets.Count, Is.EqualTo(5));
        Assert.AreEqual("A", seq?.InputSets[0].Inputs[0].ButtonName);
        Assert.That(seq?.InputSets[1].Inputs[0], Is.InstanceOf<TouchscreenInput>());
        Assert.AreEqual("B", seq?.InputSets[2].Inputs[0].ButtonName);
        Assert.That(seq?.InputSets[3].Inputs[0], Is.InstanceOf<TouchscreenDragInput>());
        Assert.AreEqual("A", seq?.InputSets[4].Inputs[0].ButtonName);
        Assert.NotNull(seq);
        Assert.AreEqual("a123,234b123,234>234,123a", seq?.ToRepresentation());
    }

    [Test]
    public void represent_keeps_analog_strength()
    {
        IInputParser parser = InputParserBuilder.FromBare()
            .Buttons("A", "B")
            .LengthRestrictions(maxSetLength: 1, maxSequenceLength: 5)
            .AnalogInputs("X", "Y")
            .Build();
        InputSequence? seq = parser.Parse("ax.1bya");
        Assert.That(seq?.InputSets.Count, Is.EqualTo(5));
        Assert.AreEqual("A", seq?.InputSets[0].Inputs[0].ButtonName);
        Assert.That(seq?.InputSets[1].Inputs[0], Is.InstanceOf<AnalogInput>());
        Assert.AreEqual("B", seq?.InputSets[2].Inputs[0].ButtonName);
        Assert.That(seq?.InputSets[3].Inputs[0], Is.InstanceOf<AnalogInput>());
        Assert.AreEqual("A", seq?.InputSets[4].Inputs[0].ButtonName);
        Assert.NotNull(seq);
        Assert.AreEqual("ax.1bya", seq?.ToRepresentation());
    }
}
