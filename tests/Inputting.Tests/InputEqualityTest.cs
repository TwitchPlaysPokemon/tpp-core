using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Inputting.Inputs;

namespace Inputting.Tests
{
    [SuppressMessage(
        category: "Assertion",
        checkId: "NUnit2009:The same value has been provided as both the actual and the expected argument",
        Justification = "This class tests equal methods, which the analysis assumes to be correctly implemented")]
    public class InputEqualityTest
    {
        private static InputSet Set(params string[] inputs) =>
            new(inputs.Select(s => new Input(s, s, s)).ToImmutableList());

        private static InputSequence Seq(params InputSet[] inputSets) => new(inputSets.ToImmutableList());

        [Test]
        public void TestSameOutcomeRegularInput()
        {
            var inputBase = new Input("foo", "a", "bar");
            var inputOtherDisplayText = new Input("X", "a", "bar");
            var inputOtherOriginalText = new Input("foo", "a", "X");
            var inputOtherEffectiveText = new Input("foo", "X", "bar");

            Assert.That(inputBase, Is.EqualTo(inputBase));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherDisplayText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherOriginalText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherEffectiveText));

            Assert.That(inputBase.HasSameOutcomeAs(inputBase), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherDisplayText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherOriginalText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherEffectiveText), Is.False);
        }

        [Test]
        public void TestSameOutcomeTouchscreenInput()
        {
            var inputBase = new TouchscreenInput("foo", "a", "bar", 11, 12);
            var inputOtherDisplayText = new TouchscreenInput("X", "a", "bar", 11, 12);
            var inputOtherOriginalText = new TouchscreenInput("foo", "a", "X", 11, 12);
            var inputOtherEffectiveText = new TouchscreenInput("foo", "X", "bar", 11, 12);
            var inputOtherCoordinateX = new TouchscreenInput("foo", "a", "bar", 999, 12);
            var inputOtherCoordinateY = new TouchscreenInput("foo", "a", "bar", 11, 999);

            Assert.That(inputBase, Is.EqualTo(inputBase as object));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherDisplayText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherOriginalText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherEffectiveText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateX));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateY));

            Assert.That(inputBase.HasSameOutcomeAs(inputBase), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherDisplayText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherOriginalText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherEffectiveText), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateX), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateY), Is.False);
        }

        [Test]
        public void TestSameOutcomeTouchscreenDragInput()
        {
            var inputBase = new TouchscreenDragInput("foo", "a", "bar", 11, 12, 13, 14);
            var inputOtherDisplayText = new TouchscreenDragInput("X", "a", "bar", 11, 12, 13, 14);
            var inputOtherOriginalText = new TouchscreenDragInput("foo", "a", "X", 11, 12, 13, 14);
            var inputOtherEffectiveText = new TouchscreenDragInput("foo", "X", "bar", 11, 12, 13, 14);
            var inputOtherCoordinateX1 = new TouchscreenDragInput("foo", "a", "bar", 999, 12, 13, 14);
            var inputOtherCoordinateY1 = new TouchscreenDragInput("foo", "a", "bar", 11, 999, 13, 14);
            var inputOtherCoordinateX2 = new TouchscreenDragInput("foo", "a", "bar", 11, 12, 999, 14);
            var inputOtherCoordinateY2 = new TouchscreenDragInput("foo", "a", "bar", 11, 12, 13, 999);

            Assert.That(inputBase, Is.EqualTo(inputBase as object));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherDisplayText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherOriginalText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherEffectiveText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateX1));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateY1));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateX2));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherCoordinateY2));

            Assert.That(inputBase.HasSameOutcomeAs(inputBase), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherDisplayText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherOriginalText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherEffectiveText), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateX1), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateY1), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateX2), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherCoordinateY2), Is.False);
        }

        [Test]
        public void TestSameOutcomeAnalogInput()
        {
            var inputBase = new AnalogInput("foo", "a", "bar", 0.1f);
            var inputOtherDisplayText = new AnalogInput("X", "a", "bar", 0.1f);
            var inputOtherOriginalText = new AnalogInput("foo", "a", "X", 0.1f);
            var inputOtherEffectiveText = new AnalogInput("foo", "X", "bar", 0.1f);
            var inputOtherStrength = new AnalogInput("foo", "a", "bar", 0.999f);

            Assert.That(inputBase, Is.EqualTo(inputBase as object));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherDisplayText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherOriginalText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherEffectiveText));
            Assert.That(inputBase, Is.Not.EqualTo(inputOtherStrength));

            Assert.That(inputBase.HasSameOutcomeAs(inputBase), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherDisplayText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherOriginalText), Is.True);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherEffectiveText), Is.False);
            Assert.That(inputBase.HasSameOutcomeAs(inputOtherStrength), Is.False);
        }

        [Test]
        public void TestSameOutcomeAnyOrder()
        {
            Assert.That(Set("a", "b"), Is.EqualTo(Set("a", "b") as object));
            Assert.That(Set("a", "b").HasSameOutcomeAs(Set("b", "a")), Is.True);

            Assert.That(Set("a", "b"), Is.Not.EqualTo(Set("b", "a")));
            Assert.That(Set("a", "b").HasSameOutcomeAs(Set("a", "x")), Is.False);

            Assert.That(Seq(Set("a", "b"), Set("c", "d")), Is.EqualTo(Seq(Set("a", "b"), Set("c", "d")) as object));
            Assert.That(Seq(Set("a", "b"), Set("c", "d")).HasSameOutcomeAs(Seq(Set("b", "a"), Set("d", "c"))), Is.True);

            Assert.That(Seq(Set("a", "b"), Set("c", "d")), Is.Not.EqualTo(Seq(Set("b", "a"), Set("d", "c"))));
            Assert.That(Seq(Set("a", "b"), Set("c", "d")).HasSameOutcomeAs(Seq(Set("a", "b"), Set("c", "x"))), Is.False);
        }

        [Test]
        public void TestSameOutcomeInputSet()
        {
            var inputRefA = new Input("Foo", "a", "foo");
            var inputRefB = new Input("Bar", "b", "bar");
            var input1A = new Input("Baz", "b", "baz");
            var input1B = new Input("Quz", "a", "quz");
            var input2 = new Input("Foo", "a", "foo");
            var input4A = new Input("Foo", "a", "foo");
            var input4B = new Input("Bar", "a", "bar");

            var setRef = new InputSet(ImmutableList.Create(inputRefA, inputRefB));
            var setDifferentOrder = new InputSet(ImmutableList.Create(input1A, input1B));
            var setDifferentLength = new InputSet(ImmutableList.Create(input2));
            var setDifferentEffectiveInput = new InputSet(ImmutableList.Create(input4A, input4B));

            Assert.That(setRef, Is.Not.EqualTo(setDifferentOrder));
            Assert.That(setRef.HasSameOutcomeAs(setDifferentOrder), Is.True);
            Assert.That(setRef.HasSameOutcomeAs(setDifferentLength), Is.False);
            Assert.That(setRef.HasSameOutcomeAs(setDifferentEffectiveInput), Is.False);
        }
    }
}
