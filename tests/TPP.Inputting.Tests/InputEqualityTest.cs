using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using TPP.Inputting.Inputs;

namespace TPP.Inputting.Tests;

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

        Assert.AreEqual(inputBase, inputBase);
        Assert.AreNotEqual(inputBase, inputOtherDisplayText);
        Assert.AreNotEqual(inputBase, inputOtherOriginalText);
        Assert.AreNotEqual(inputBase, inputOtherEffectiveText);

        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputBase));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherDisplayText));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherOriginalText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherEffectiveText));
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

        Assert.AreEqual(inputBase, inputBase);
        Assert.AreNotEqual(inputBase, inputOtherDisplayText);
        Assert.AreNotEqual(inputBase, inputOtherOriginalText);
        Assert.AreNotEqual(inputBase, inputOtherEffectiveText);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateX);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateY);

        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputBase));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherDisplayText));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherOriginalText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherEffectiveText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateX));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateY));
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

        Assert.AreEqual(inputBase, inputBase);
        Assert.AreNotEqual(inputBase, inputOtherDisplayText);
        Assert.AreNotEqual(inputBase, inputOtherOriginalText);
        Assert.AreNotEqual(inputBase, inputOtherEffectiveText);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateX1);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateY1);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateX2);
        Assert.AreNotEqual(inputBase, inputOtherCoordinateY2);

        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputBase));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherDisplayText));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherOriginalText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherEffectiveText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateX1));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateY1));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateX2));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherCoordinateY2));
    }

    [Test]
    public void TestSameOutcomeAnalogInput()
    {
        var inputBase = new AnalogInput("foo", "a", "bar", 0.1f);
        var inputOtherDisplayText = new AnalogInput("X", "a", "bar", 0.1f);
        var inputOtherOriginalText = new AnalogInput("foo", "a", "X", 0.1f);
        var inputOtherEffectiveText = new AnalogInput("foo", "X", "bar", 0.1f);
        var inputOtherStrength = new AnalogInput("foo", "a", "bar", 0.999f);

        Assert.AreEqual(inputBase, inputBase);
        Assert.AreNotEqual(inputBase, inputOtherDisplayText);
        Assert.AreNotEqual(inputBase, inputOtherOriginalText);
        Assert.AreNotEqual(inputBase, inputOtherEffectiveText);
        Assert.AreNotEqual(inputBase, inputOtherStrength);

        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputBase));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherDisplayText));
        Assert.IsTrue(inputBase.HasSameOutcomeAs(inputOtherOriginalText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherEffectiveText));
        Assert.IsFalse(inputBase.HasSameOutcomeAs(inputOtherStrength));
    }

    [Test]
    public void TestSameOutcomeAnyOrder()
    {
        Assert.AreEqual(Set("a", "b"), Set("a", "b"));
        Assert.IsTrue(Set("a", "b").HasSameOutcomeAs(Set("b", "a")));

        Assert.AreNotEqual(Set("a", "b"), Set("b", "a"));
        Assert.IsFalse(Set("a", "b").HasSameOutcomeAs(Set("a", "x")));

        Assert.AreEqual(Seq(Set("a", "b"), Set("c", "d")), Seq(Set("a", "b"), Set("c", "d")));
        Assert.IsTrue(Seq(Set("a", "b"), Set("c", "d")).HasSameOutcomeAs(Seq(Set("b", "a"), Set("d", "c"))));

        Assert.AreNotEqual(Seq(Set("a", "b"), Set("c", "d")), Seq(Set("b", "a"), Set("d", "c")));
        Assert.IsFalse(Seq(Set("a", "b"), Set("c", "d")).HasSameOutcomeAs(Seq(Set("a", "b"), Set("c", "x"))));
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

        Assert.AreNotEqual(setRef, setDifferentOrder);
        Assert.IsTrue(setRef.HasSameOutcomeAs(setDifferentOrder));
        Assert.IsFalse(setRef.HasSameOutcomeAs(setDifferentLength));
        Assert.IsFalse(setRef.HasSameOutcomeAs(setDifferentEffectiveInput));
    }
}
