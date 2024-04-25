using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using TPP.Core;
using TPP.Core.Overlay.Events;
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

    private void AssertNotEmptyMap(string rawInput, string? message = null)
    {
        var mappedInputs = _inputMapper.Map(new TimedInputSet(ParseInput(rawInput), 1, 2));
        Assert.That(mappedInputs.Keys, Has.Count.GreaterThan(2), message ?? "Mapped output should contain buttons.");
    }

    [Test]
    public void TestBuildEveryProfile()
    {
        foreach (string profile in Enum.GetNames<ButtonProfile>())
        {
            Assert.DoesNotThrow(() => Enum.Parse<ButtonProfile>(profile).ToInputParserBuilder().Build(), $"Button Profile {profile} failed to build.");
        }
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
        AssertMapped("n", "Cup");
        AssertMapped("dn", "Up");
        AssertMapped("south", "Cdown");
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
    public void TestSNES2NES()
    {
        _inputParser = ButtonProfile.SNEStoNES.ToInputParserBuilder().Build();

        // good cases
        AssertMapped("a", "B"); // SNES B maps to NES A
        AssertMapped("b", "Y"); // SNES Y maps to NES B

        // bad cases
        AssertEmptyMap("x", "SNES X has no NES equivalent");
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

    [Test]
    public void TestCardinalInputs()
    {
        Assert.Multiple(() =>
        {
            foreach (string profile in Enum.GetNames<ButtonProfile>())
            {
                var parserBuilder = Enum.Parse<ButtonProfile>(profile).ToInputParserBuilder();
                _inputParser = parserBuilder.Build();
                if (_inputParser != null)
                {
                    foreach (string prefix in parserBuilder.PadStickPrefixes)
                    {
                        AssertNotEmptyMap($"{prefix}n", $"{profile} profile has {prefix}up but not {prefix}n");
                        AssertNotEmptyMap($"{prefix}e", $"{profile} profile has {prefix}right but not {prefix}e");
                        AssertNotEmptyMap($"{prefix}w", $"{profile} profile has {prefix}left but not {prefix}w");
                        AssertNotEmptyMap($"{prefix}s", $"{profile} profile has {prefix}down but not {prefix}s");
                        AssertNotEmptyMap($"{prefix}north", $"{profile} profile has {prefix}up but not {prefix}north");
                        AssertNotEmptyMap($"{prefix}east", $"{profile} profile has {prefix}right but not {prefix}east");
                        AssertNotEmptyMap($"{prefix}west", $"{profile} profile has {prefix}left but not {prefix}west");
                        AssertNotEmptyMap($"{prefix}south", $"{profile} profile has {prefix}down but not {prefix}south");
                    }
                }
            }
        });
    }

    private static Model.User MockUser(
    string name = "user",
    int pokeyen = 0,
    int tokens = 0,
    string? twitchDisplayName = null,
    int? pokeyenBetRank = null,
    bool glowColorUnlocked = false,
    SortedSet<int>? emblems = null
    ) => new Model.User(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: twitchDisplayName ?? "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0),
        lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null,
        pokeyen: pokeyen, tokens: tokens,
        pokeyenBetRank: pokeyenBetRank, glowColorUnlocked: glowColorUnlocked,
        participationEmblems: emblems);

    [Test]
    public void TestOverlayTouchscreenSupport()
    {
        _inputParser = ButtonProfile.Nintendo3DS.ToInputParserBuilder().Build();

        var buttonsOnly = new NewAnarchyInput(1, ParseInput("a"), MockUser(), null, null);
        var touch = new NewAnarchyInput(1, ParseInput("20,40"), MockUser(), null, null);
        var drag = new NewAnarchyInput(1, ParseInput("20,40>50,60"), MockUser(), null, null);
        Assert.That(buttonsOnly.X == null && buttonsOnly.Y == null && buttonsOnly.X2 == null && buttonsOnly.Y2 == null, "Buttons should create no touchscreen coordinates");
        Assert.That(touch.X == 20 && touch.Y == 40 && touch.X2 == null && touch.Y2 == null, "Touchscreen coordinates should be mapped to x,y properties");
        Assert.That(drag.X == 20 && drag.Y == 40 && drag.X2 == 50 && drag.Y2 == 60, "Drag coordinates should be mapped to x,y,x2,y2 properties");
    }

    [Test]
    public void TestOverlayAnalogInputs()
    {
        _inputParser = ButtonProfile.Switch.ToInputParserBuilder().Build();

        var fullThrow = new NewAnarchyInput(1, ParseInput("ln"), MockUser(), null, null);
        var halfThrow = new NewAnarchyInput(1, ParseInput("ln.5"), MockUser(), null, null);
        var buttonPress = new NewAnarchyInput(1, ParseInput("a"), MockUser(), null, null);
        Assert.That(fullThrow.ButtonSetLabels.SequenceEqual(["lup"]) && fullThrow.ButtonSetVelocities.SequenceEqual([1f]), "Full throw should have a velocity of 1");
        Assert.That(halfThrow.ButtonSetLabels.SequenceEqual(["lup"]) && halfThrow.ButtonSetVelocities.SequenceEqual([0.5f]), "Half throw should have a velocity of 0.5");
        Assert.That(buttonPress.ButtonSetLabels.SequenceEqual(["a"]) && buttonPress.ButtonSetVelocities.SequenceEqual([1f]), "Non-analog should have a velocity of 1");

    }

    [Test]
    public void TestCardinalInputDisplayMapping()
    {
        Assert.Multiple(() =>
        {
            foreach (string profile in Enum.GetNames<ButtonProfile>())
            {
                var parserBuilder = Enum.Parse<ButtonProfile>(profile).ToInputParserBuilder();
                _inputParser = parserBuilder.Build();
                if (_inputParser != null)
                {
                    foreach (string prefix in parserBuilder.PadStickPrefixes)
                    {
                        var northEast = new NewAnarchyInput(1, ParseInput($"{prefix}north+{prefix}e"), MockUser(), null, null);
                        var southWest = new NewAnarchyInput(1, ParseInput($"{prefix}s+{prefix}west"), MockUser(), null, null);
                        if (northEast.ButtonSet.Any() && southWest.ButtonSet.Any())
                        {
                            Assert.That(northEast.ButtonSetLabels.Any(b => b.ToLower() == $"{prefix}up")
                                && northEast.ButtonSetLabels.Any(b => b.ToLower() == $"{prefix}right")
                                && southWest.ButtonSetLabels.Any(b => b.ToLower() == $"{prefix}down")
                                && southWest.ButtonSetLabels.Any(b => b.ToLower() == $"{prefix}left"),
                                $"NSEW labels with '{prefix}' prefix aren't converted to {prefix}up {prefix}down {prefix}right {prefix}left for {profile} profile");
                        }
                    }
                }
            }
        });
    }
}
