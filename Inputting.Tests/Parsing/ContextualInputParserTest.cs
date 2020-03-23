using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Inputting.InputDefinitions;
using Inputting.Parsing;
using NUnit.Framework;

namespace Inputting.Tests.Parsing
{
    public class ContextualInputParserTest
    {
        private static InputSet Set(params string[] inputs) =>
            new InputSet(inputs.Select(s => new Input(s, s, s, true)));

        private static InputSequence Seq(params InputSet[] inputSets) => new InputSequence(inputSets);

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
                .DPad(prefix: "")
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

            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", new TouchCoords(234, 123)),
                    new Input("11,22", "touch", "11,22", new TouchCoords(11, 22)),
                })
            ), _inputParser.Parse("234,123+11,22"));
            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", new TouchCoords(234, 123)),
                    new Input("11,22>33,44", "drag", "11,22>33,44", new TouchDragCoords(11, 22, 33, 44)),
                })
            ), _inputParser.Parse("234,123+11,22>33,44"));

            // multitouch enabled
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
                .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 4)
                .Build();

            Assert.IsNull(_inputParser.Parse("a+a"));
            Assert.IsNull(_inputParser.Parse("up+up.1"));
            Assert.IsNull(_inputParser.Parse("n+up"));
            Assert.IsNull(_inputParser.Parse("11,22+11,22"));
            Assert.IsNull(_inputParser.Parse("11,22>50,60+11,22>50,60"));
        }

        [Test]
        public void TestPerformance()
        {
            _inputParser = InputParserBuilder.FromBare()
                .Buttons("a", "b", "start", "select", "wait")
                .DPad(prefix: "")
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
    }
}
