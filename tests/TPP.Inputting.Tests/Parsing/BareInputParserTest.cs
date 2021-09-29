using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests.Parsing;

public class BareInputParserTest
{
    private static InputSet Set(params string[] inputs) =>
        new(inputs.Select(s => new Input(s, s, s)).ToImmutableList());

    private static InputSequence Seq(params InputSet[] inputSets) => new(inputSets.ToImmutableList());

    private IInputParser _inputParser = null!;

    private void AssertInput(string rawInput, InputSequence? expectedSequence)
    {
        Assert.AreEqual(expectedSequence, _inputParser.Parse(rawInput));
    }

    [Test]
    public void TestBasicInputs()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a", "b", "start", "select")
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        // good cases
        AssertInput("aa", Seq(Set("a"), Set("a")));
        AssertInput("a2a", Seq(Set("a"), Set("a"), Set("a")));
        AssertInput("a2a2", Seq(Set("a"), Set("a"), Set("a"), Set("a")));
        AssertInput("start4", Seq(Set("start"), Set("start"), Set("start"), Set("start")));
        AssertInput("a+b2startselect", Seq(Set("a", "b"), Set("a", "b"), Set("start"), Set("select")));
        AssertInput("b+a", Seq(Set("b", "a"))); // order is preserved

        // bad cases
        AssertInput("x", null); // not a button
        AssertInput("a+b+start", null); // set too long
        AssertInput("a5", null); // sequence too long
    }

    [Test]
    public void TestHold()
    {
        InputParserBuilder builder = InputParserBuilder.FromBare()
            .Buttons("a")
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 1);

        // hold enabled
        _inputParser = builder.HoldEnabled(true).Build();
        AssertInput("a", Seq(Set("a")));
        Assert.AreEqual(Seq(new InputSet(ImmutableList.Create(
            new Input("a", "a", "a"),
            HoldInput.Instance))
        ), _inputParser.Parse("a-"));

        // hold disabled
        _inputParser = builder.HoldEnabled(false).Build();
        AssertInput("a", Seq(Set("a")));
        AssertInput("a-", null);
    }

    [Test]
    public void TestAlias()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("y")
            .AliasedButtons(("honk", "y"))
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        InputSequence? result = _inputParser.Parse("honky");
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create(new Input("honk", "y", "honk"))),
            new InputSet(ImmutableList.Create(new Input("y", "y", "y")))
        ), result);
    }

    [Test]
    public void TestRemapping()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("y")
            .RemappedButtons(("honk", "y"))
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        InputSequence? result = _inputParser.Parse("honky");
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create(new Input("y", "y", "honk"))),
            new InputSet(ImmutableList.Create(new Input("y", "y", "y")))
        ), result);
    }

    [Test]
    public void TestTouchscreen()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .Touchscreen(width: 240, height: 160, multitouch: false, allowDrag: false)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a"))),
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("234,123", "touchscreen", "234,123", 234, 123))),
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a")))
        ), _inputParser.Parse("a234,123a"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("239,159", "touchscreen", "239,159", 239, 159)))
        ), _inputParser.Parse("239,159"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("0,0", "touchscreen", "0,0", 0, 0)))
        ), _inputParser.Parse("0,0"));
        // allow leading zeroes
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("000,000", "touchscreen", "000,000", 0, 0)))
        ), _inputParser.Parse("000,000"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("012,023", "touchscreen", "012,023", 12, 23)))
        ), _inputParser.Parse("012,023"));
        // out of bounds
        Assert.IsNull(_inputParser.Parse("240,159"));
        Assert.IsNull(_inputParser.Parse("239,160"));
        // reject non-ascii decimal number symbols
        Assert.IsNull(_inputParser.Parse("১২৩,꧑꧒꧓"));
    }

    [Test]
    public void TestTouchscreenDrag()
    {
        // drag enabled
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .Touchscreen(width: 240, height: 160, multitouch: false, allowDrag: true)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a"))),
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenDragInput("234,123>222,111", "touchscreen", "234,123>222,111", 234, 123, 222, 111))),
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a")))
        ), _inputParser.Parse("a234,123>222,111a"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenDragInput("239,159>0,0", "touchscreen", "239,159>0,0", 239, 159, 0, 0)))
        ), _inputParser.Parse("239,159>0,0"));
        // allow leading zeroes
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenDragInput("000,000>000,000", "touchscreen", "000,000>000,000", 0, 0, 0, 0)))
        ), _inputParser.Parse("000,000>000,000"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenDragInput("012,023>0,0", "touchscreen", "012,023>0,0", 12, 23, 0, 0)))
        ), _inputParser.Parse("012,023>0,0"));
        // out of bounds
        Assert.IsNull(_inputParser.Parse("240,159>0,0"));
        Assert.IsNull(_inputParser.Parse("239,160>0,0"));
        Assert.IsNull(_inputParser.Parse("0,0>239,160"));
        Assert.IsNull(_inputParser.Parse("0,0>239,160"));
        // reject non-ascii decimal number symbols
        Assert.IsNull(_inputParser.Parse("১২৩,꧑꧒꧓>৪৫,꧔꧕"));

        // drag disabled
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .Touchscreen(width: 240, height: 160, multitouch: false, allowDrag: false)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.IsNull(_inputParser.Parse("a234,123>222,111a"));
        Assert.IsNull(_inputParser.Parse("239,159>0,0"));
    }

    [Test]
    public void TestTouchscreenAlias()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .Touchscreen(width: 240, height: 160, multitouch: false, allowDrag: false)
            .AliasedTouchscreenInput("move1", x: 42, y: 69)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a"))),
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("move1", "touchscreen", "move1", 42, 69))),
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("move1", "touchscreen", "move1", 42, 69))),
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a")))
        ), _inputParser.Parse("amove12a"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("234,123", "touchscreen", "234,123", 234, 123))),
            new InputSet(ImmutableList.Create<Input>(
                new TouchscreenInput("move1", "touchscreen", "move1", 42, 69)))
        ), _inputParser.Parse("234,123move1"));
        Assert.IsNull(_inputParser.Parse("234,123+move1"));
    }

    [Test]
    public void TestAnalog()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .AnalogInputs("up")
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a"))),
            new InputSet(ImmutableList.Create<Input>(new AnalogInput("up", "up", "up.2", 0.2f))),
            new InputSet(ImmutableList.Create<Input>(new AnalogInput("up", "up", "up.2", 0.2f))),
            new InputSet(ImmutableList.Create<Input>(new Input("a", "a", "a")))
        ), _inputParser.Parse("aup.22a"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new AnalogInput("up", "up", "UP", 1.0f)))
        ), _inputParser.Parse("UP"));
        Assert.AreEqual(Seq(
            new InputSet(ImmutableList.Create<Input>(new AnalogInput("up", "up", "up.9", 0.9f)))
        ), _inputParser.Parse("up.9"));
        Assert.IsNull(_inputParser.Parse("up."));
        Assert.IsNull(_inputParser.Parse("up.0"));
        Assert.IsNull(_inputParser.Parse("up.10"));
    }

    [Test]
    public void TestRetainCase()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("A")
            .AliasedButtons(("B", "X"))
            .RemappedButtons(("C", "Y"))
            .AnalogInputs("UP")
            .AliasedTouchscreenInput("MOVE1", 10, 20)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(
            Seq(new InputSet(ImmutableList.Create(new Input("A", "A", "a")))), _inputParser.Parse("a"));
        Assert.AreEqual(
            Seq(new InputSet(ImmutableList.Create(new Input("B", "X", "b")))), _inputParser.Parse("b"));
        Assert.AreEqual(
            Seq(new InputSet(ImmutableList.Create(new Input("Y", "Y", "c")))), _inputParser.Parse("c"));
        Assert.AreEqual(
            Seq(new InputSet(ImmutableList.Create<Input>(new AnalogInput("UP", "UP", "up.2", 0.2f)))),
            _inputParser.Parse("up.2"));
        Assert.AreEqual(
            Seq(new InputSet(
                ImmutableList.Create<Input>(new TouchscreenInput("MOVE1", "touchscreen", "move1", 10, 20)))),
            _inputParser.Parse("move1"));
    }
}
