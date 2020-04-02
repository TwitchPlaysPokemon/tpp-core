using System.Collections.Generic;
using System.Linq;
using Inputting.InputDefinitions;
using Inputting.Parsing;
using NUnit.Framework;

namespace Inputting.Tests.Parsing
{
    public class BareInputParserTest
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
            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {new Input("a", "a", "a", true), new Input("hold", "hold", "-", true)})
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
                new InputSet(new List<Input> {new Input("honk", "y", "honk", true)}),
                new InputSet(new List<Input> {new Input("y", "y", "y", true)})
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
                new InputSet(new List<Input> {new Input("y", "y", "honk", true)}),
                new InputSet(new List<Input> {new Input("y", "y", "y", true)})
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
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", additionalData: new TouchCoords(234, 123))
                }),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("a234,123a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input>
                {
                    new Input("239,159", "touch", "239,159", additionalData: new TouchCoords(239, 159))
                })
            ), _inputParser.Parse("239,159"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input>
                {
                    new Input("0,0", "touch", "0,0", additionalData: new TouchCoords(0, 0))
                })
            ), _inputParser.Parse("0,0"));
            Assert.IsNull(_inputParser.Parse("240,159"));
            Assert.IsNull(_inputParser.Parse("239,160"));
            // disallow leading zeroes
            Assert.IsNull(_inputParser.Parse("0000,0000"));
            Assert.IsNull(_inputParser.Parse("012,023"));
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
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input>
                {
                    new Input("234,123>222,111", "drag", "234,123>222,111",
                        additionalData: new TouchDragCoords(234, 123, 222, 111))
                }),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("a234,123>222,111a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input>
                {
                    new Input("239,159>0,0", "drag", "239,159>0,0",
                        additionalData: new TouchDragCoords(239, 159, 0, 0))
                })
            ), _inputParser.Parse("239,159>0,0"));
            Assert.IsNull(_inputParser.Parse("240,159>0,0"));
            Assert.IsNull(_inputParser.Parse("239,160>0,0"));
            Assert.IsNull(_inputParser.Parse("0,0>239,160"));
            Assert.IsNull(_inputParser.Parse("0,0>239,160"));
            // disallow leading zeroes
            Assert.IsNull(_inputParser.Parse("0000,0000>0000,0000"));
            Assert.IsNull(_inputParser.Parse("012,023>0,0"));

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
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input> {new Input("move1", "touch", "move1", new TouchCoords(42, 69))}),
                new InputSet(new List<Input> {new Input("move1", "touch", "move1", new TouchCoords(42, 69))}),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("amove12a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("234,123", "touch", "234,123", new TouchCoords(234, 123))}),
                new InputSet(new List<Input> {new Input("move1", "touch", "move1", new TouchCoords(42, 69))})
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
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input> {new Input("up", "up", "up.2", 0.2f)}),
                new InputSet(new List<Input> {new Input("up", "up", "up.2", 0.2f)}),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("aup.22a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("up", "up", "UP", 1.0f)})
            ), _inputParser.Parse("UP"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("up", "up", "up.9", 0.9f)})
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
                Seq(new InputSet(new[] {new Input("A", "A", "a", true)})),
                _inputParser.Parse("a"));
            Assert.AreEqual(
                Seq(new InputSet(new[] {new Input("B", "X", "b", true)})),
                _inputParser.Parse("b"));
            Assert.AreEqual(
                Seq(new InputSet(new[] {new Input("Y", "Y", "c", true)})),
                _inputParser.Parse("c"));
            Assert.AreEqual(
                Seq(new InputSet(new[] {new Input("UP", "UP", "up.2", 0.2f)})),
                _inputParser.Parse("up.2"));
            Assert.AreEqual(
                Seq(new InputSet(new[] {new Input("MOVE1", "touch", "move1", new TouchCoords(10, 20))})),
                _inputParser.Parse("move1"));
        }
    }
}
