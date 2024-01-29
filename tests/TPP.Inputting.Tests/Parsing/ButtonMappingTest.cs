using System.Collections.Immutable;
using NUnit.Framework;
using TPP.Core;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests.Parsing;
public class ButtonMappingTest
{
    private IInputMapper _inputMapper = new DefaultTppInputMapper();

    private IInputParser _inputParser = null!;

    private InputSet ParseInput(string inputStr)
    {
        InputSequence? inputSequence = _inputParser.Parse(inputStr);
        return inputSequence?.InputSets[0] ?? new InputSet(ImmutableList<Input>.Empty);
    }

    private void AssertMapped(string rawInput, params string[] buttons)
    {
        var mappedInputs = _inputMapper.Map(new TimedInputSet(ParseInput(rawInput), 1, 2));
        foreach (var button in buttons)
        {
            Assert.That(mappedInputs.ContainsKey(button), Is.True, $"Output should contain {button}.");
        }
    }

    private void AssertEmptyMap(string rawInput, string? message = null)
    {
        var mappedInputs = _inputMapper.Map(new TimedInputSet(ParseInput(rawInput), 1, 2));
        Assert.That(mappedInputs.Keys, Has.Count.EqualTo(2), message ?? "Mapped output should contain no buttons.");
    }

    [Test]
    public void TestGameBoy()
    {
        _inputParser = ButtonProfile.GameBoy.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("n", "Up");
        AssertMapped("south", "Down");

        // bad cases
        AssertEmptyMap("x"); // not a button
        AssertEmptyMap("a+b+start+select", "Soft Reset should be blocked."); // Soft Reset
    }

    [Test]
    public void TestGameBoyAdvance()
    {
        _inputParser = ButtonProfile.GameBoyAdvance.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l+r", "L", "R");
        AssertMapped("n", "Up");
        AssertMapped("south", "Down");

        // bad cases
        AssertEmptyMap("x"); // not a button
        AssertEmptyMap("a+b+start+select", "Soft Reset should be blocked.");
    }

    [Test]
    public void TestNintendoDS()
    {
        _inputParser = ButtonProfile.NintendoDS.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l+r", "L", "R");
        AssertMapped("x+y", "X", "Y");
        AssertMapped("n", "Up");
        AssertMapped("south", "Down");
        AssertMapped("10,100>80,120", "Touch_Screen_X", "Touch_Screen_Y", "Touch_Screen_X2", "Touch_Screen_Y2");

        // bad cases
        AssertEmptyMap("300,200", "Out of bounds touch should be blocked.");
        AssertEmptyMap("l+r+start+select", "Soft Reset should be blocked.");
    }

    [Test]
    public void TestNintendo3DS()
    {
        _inputParser = ButtonProfile.Nintendo3DS.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l+r", "L", "R");
        AssertMapped("x+y", "X", "Y");
        AssertMapped("n", "Up");
        AssertMapped("dn", "Up");
        AssertMapped("south", "Down");
        AssertMapped("cn", "Cup");
        AssertMapped("cspinl", "Cspinl");
        AssertMapped("10,100>80,120", "Touch_Screen_X", "Touch_Screen_Y", "Touch_Screen_X2", "Touch_Screen_Y2");

        // bad cases
        AssertEmptyMap("400,200", "Out of bounds touch should be blocked.");
        AssertEmptyMap("l+r+start", "Soft Reset should be blocked.");
        AssertEmptyMap("l+r+select", "Soft Reset should be blocked.");
        AssertEmptyMap("l+s+select", "Luma3DS menu shortcut should be blocked.");
        AssertEmptyMap("l+start", "NTR menu shortcut should be blocked.");
    }

    [Test]
    public void TestN64()
    {
        _inputParser = ButtonProfile.N64.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l+r+z", "L", "R", "Z");
        AssertMapped("n", "Up");
        AssertMapped("aw.5", "Aleft");
        AssertMapped("ln+cs+cright", "Aup", "Cdown", "Cright");
        AssertMapped("south", "Down");

        // bad cases
        AssertEmptyMap("x+y");
        AssertEmptyMap("b+x+start", "XD Soft Reset went through");
    }

    [Test]
    public void TestGameCube()
    {
        _inputParser = ButtonProfile.GameCube.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l+r", "L", "R");
        AssertMapped("x+y", "X", "Y");
        AssertMapped("n", "Up");
        AssertMapped("ln.5", "Lup");
        AssertMapped("ln+cs+rright", "Lup", "Rdown", "Rright");
        AssertMapped("south", "Down");

        // bad cases
        AssertEmptyMap("b+x+start", "XD Soft Reset went through");
    }

    [Test]
    public void TestSwitch()
    {
        _inputParser = ButtonProfile.Switch.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a+b", "A", "B");
        AssertMapped("l2+zr+r3", "Zl", "Zr", "Rstick");
        AssertMapped("x+y", "X", "Y");
        AssertMapped("+", "Plus");
        AssertMapped("start+minus", "Plus", "Minus");
        AssertMapped("n", "Up");
        AssertMapped("dn", "Up");
        AssertMapped("ln.5", "Lup");
        AssertMapped("ln+cs+rright", "Lup", "Rdown", "Rright");
        AssertMapped("lspinl", "Lspinl");
        AssertMapped("lsouth", "Ldown");

        // bad cases
        AssertEmptyMap("capture+home", "Capture and Home buttons should not be mapped.");
    }

    [Test]
    public void TestDualSNES()
    {
        _inputParser = ButtonProfile.DualSNES.ToInputParserBuilder().Build();
        if (_inputParser is SidedInputParser and not null)
        {
            ((SidedInputParser)_inputParser).AllowDirectedInputs = true;
        }

        // good cases
        AssertMapped("lstart", "P1 Start");
        AssertMapped("r:b+n", "P2 B", "P2 Up");

        // bad cases
        AssertEmptyMap("l:a+b+start+select", "Soft Reset should be blocked.");
    }

    [Test]
    public void TestConfiguredPrefixes()
    {
        var originalMapper = _inputMapper;
        try
        {
            _inputMapper = new DefaultTppInputMapper(controllerPrefixes: new string[] { "Left ", "Right " });
            _inputParser = ButtonProfile.GameBoy.ToInputParserBuilder().Build();

            // good cases
            AssertMapped("up", "Left Up");

            _inputParser = ButtonProfile.DualGameBoy.ToInputParserBuilder().Build();
            if (_inputParser is SidedInputParser and not null)
            {
                ((SidedInputParser)_inputParser).AllowDirectedInputs = true;
            }

            // good cases
            AssertMapped("lstart", "Left Start");
            AssertMapped("r:b+n", "Right B", "Right Up");
        }
        finally
        {
            _inputMapper = originalMapper;
        }
    }
}
