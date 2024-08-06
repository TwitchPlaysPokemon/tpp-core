using NUnit.Framework;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests.Parsing;

public class SidedInputParserTest
{
    private static Input Input(string input) => new(input, input, input);

    [Test]
    public void TestPreferValidInputOverSidePrefix()
    {
        IInputParser inputParser = InputParserBuilder.FromBare()
            .Buttons("up", "rup")
            .LeftRightSidesEnabled(true)
            .Build();
        ((SidedInputParser)inputParser).AllowDirectedInputs = true;

        Assert.That(inputParser.Parse("up"),
            Is.EqualTo(new InputSequence([new InputSet([Input("up"), new SideInput(null, false)])])));
        Assert.That(inputParser.Parse("rup"),
            Is.EqualTo(new InputSequence([new InputSet([Input("rup"), new SideInput(null, false)])])));
        Assert.That(inputParser.Parse("rrup"),
            Is.EqualTo(new InputSequence([new InputSet([Input("rup"), new SideInput(InputSide.Right, true)])])));
    }
}
