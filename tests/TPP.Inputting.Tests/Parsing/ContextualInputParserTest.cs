using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests.Parsing;

public class ContextualInputParserTest
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
    public void TestConflicts()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .DPad()
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        AssertInput("aupdown", Seq(Set("a"), Set("up"), Set("down")));
        AssertInput("aup+down", null);
        AssertInput("adown+up", null);
        AssertInput("aupleft", Seq(Set("a"), Set("up"), Set("left")));
        AssertInput("aup+left", Seq(Set("a"), Set("up", "left")));
    }

    [Test]
    public void TestMultitouch()
    {
        // multitouch enabled
        _inputParser = InputParserBuilder.FromBare()
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Touchscreen(width: 240, height: 160, multitouch: true, allowDrag: true)
            .Build();

        Assert.AreEqual(Seq(new InputSet(ImmutableList.Create<Input>(
            new TouchscreenInput("234,123", "touchscreen", "234,123", 234, 123),
            new TouchscreenInput("11,22", "touchscreen", "11,22", 11, 22)))
        ), _inputParser.Parse("234,123+11,22"));
        Assert.AreEqual(Seq(new InputSet(ImmutableList.Create<Input>(
            new TouchscreenInput("234,123", "touchscreen", "234,123", 234, 123),
            new TouchscreenDragInput("11,22>33,44", "touchscreen", "11,22>33,44", 11, 22, 33, 44)))
        ), _inputParser.Parse("234,123+11,22>33,44"));

        // multitouch disabled
        _inputParser = InputParserBuilder.FromBare()
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Touchscreen(width: 240, height: 160, multitouch: false, allowDrag: true)
            .Build();

        Assert.IsNull(_inputParser.Parse("234,123+11,22"));
        Assert.IsNull(_inputParser.Parse("234,123+11,22>33,44"));
        Assert.IsNull(_inputParser.Parse("234,123>0,0+11,22>33,44"));
    }

    [Test]
    public void TestRejectDuplicates()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a")
            .AliasedButtons(("n", "up"))
            .AnalogInputs("up")
            .Touchscreen(width: 240, height: 160, multitouch: true, allowDrag: true)
            .AliasedTouchscreenInput("move1", 11, 22)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.IsNull(_inputParser.Parse("a+a"));
        Assert.IsNull(_inputParser.Parse("up+up.1"));
        Assert.IsNull(_inputParser.Parse("n+up"));
        Assert.IsNull(_inputParser.Parse("11,22+11,22"));
        Assert.IsNull(_inputParser.Parse("11,22>50,60+11,22>50,60"));
        Assert.IsNull(_inputParser.Parse("11,22+move1"));
    }

    [Test]
    public void TestPerformance()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a", "b", "start", "select", "wait")
            .DPad()
            .RemappedDPad(up: "n", down: "s", left: "w", right: "e", mapsToPrefix: "")
            .AnalogStick(prefix: "c", allowSpin: true)
            .RemappedButtons(("p", "wait"), ("xp", "wait"), ("exp", "wait"))
            .AliasedButtons(("honk", "y"))
            .Touchscreen(width: 240, height: 160, multitouch: true, allowDrag: true)
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .HoldEnabled(true)
            .Build();

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _inputParser.Parse("a");
            _inputParser.Parse("A");
            _inputParser.Parse("aa");
            _inputParser.Parse("123,234");
            _inputParser.Parse("11,22>33,44");
            _inputParser.Parse("a+b2startSelect");
            _inputParser.Parse("cupupcup.2up");
            _inputParser.Parse("p");
            _inputParser.Parse("phonk+left-");
            _inputParser.Parse("start9");
        }
        stopwatch.Stop();
        // check for 1k inputs per second. I get around 30k inputs per second on an i5-7600k @ 3.80GHz,
        // but this test should only fail if something got horribly slow.
        Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void TestConflictWithWait()
    {
        _inputParser = InputParserBuilder.FromBare()
            .Buttons("a", "wait")
            .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
            .Build();

        Assert.AreEqual(Seq(Set("a"), Set("wait")), _inputParser.Parse("await"));
        Assert.IsNull(_inputParser.Parse("a+wait"));
    }
}
