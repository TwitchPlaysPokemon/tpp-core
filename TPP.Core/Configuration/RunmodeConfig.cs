using System;
using System.Collections.Generic;
using System.ComponentModel;
using TPP.Inputting;

namespace TPP.Core.Configuration;

public class RunmodeConfig : ConfigBase, IRootConfig
{
    public string Schema => "./config.runmode.schema.json";

    [Description("Host of the HTTP input server where inputs can be polled from.")]
    public string InputServerHost { get; init; } = "127.0.0.1";
    [Description("Port of the HTTP input server where inputs can be polled from.")]
    public int InputServerPort { get; init; } = 5010;

    public InputConfig InputConfig { get; init; } = new();

    [Description("If this is not null, any user participating will get that run number's participation emblem.")]
    public int? RunNumber = null;

    [Description("If true, inputs are muted until explicitly unmuted. " +
                 "Muting also implies that no emblems are handed out and no run statistics are recorded. " +
                 "Inputs can be unmuted at runtime using !unmuteinputs or calling /start_run on the inputserver. " +
                 "Inputs can be muted at runtime using !muteinputs or calling /stop_run on the inputserver. ")]
    public bool MuteInputsAtStartup = false;

    [Description("If not null, this amount of time must pass before a player can switch sides again." +
                 "Only relevant for dual-sided input profiles.")]
    public TimeSpan? SwitchSidesCooldown { get; init; } = null;

    [Description("If true, auto assigns users a side if they haven't picked one yet. " +
                 "Otherwise it would flip-flop their inputs between sides.")]
    public bool AutoAssignSide { get; init; } = false;
}

/// Contains all parameters that affect how inputs are performed.
public sealed class InputConfig : ConfigBase
{
    public ButtonProfile ButtonsProfile { get; set; } = ButtonProfile.GameBoy;
    public int FramesPerSecond { get; init; } = 60;

    // input duration timings
    public int MinSleepFrames { get; init; } = 1;
    public int MinPressFrames { get; init; } = 1;
    public int MaxPressFrames { get; init; } = 16;
    public int MaxHoldFrames { get; init; } = 120;

    // anarchy input queue
    public float BufferLengthSeconds { get; init; } = 3f;
    public float SpeedupRate { get; init; } = 0.2f;
    public float SlowdownRate { get; init; } = 1f;
    public int MinInputFrames { get; init; } = 1;
    public int MaxInputFrames { get; init; } = 100;
    public int MaxBufferLength { get; init; } = 1000;

    [Description("Whether players can choose an input side per-input using a side prefix, e.g. 'left:'. " +
                 "Disabling this typically means you expect players to choose a side using e.g. !left or !right. " +
                 "This has no effect for non-dual-sided input profiles.")]
    public bool AllowDirectedInputs { get; init; } = true;
    public bool AllowHeldInputs { get; init; } = true;
    public int MaxSetLength { get; init; } = 0;
    public int MaxSequenceLength { get; init; } = 0;
    public string? ControllerPrefix { get; set; } = null;
    public string? ControllerPrefix2 { get; set; } = null;

    [Description("Defines shorthand aliases for touchscreen coordinates, e.g. \"move1\": [1, 25]")]
    public Dictionary<string, uint[]> TouchscreenAliases = [];

    [Description("Includes predetermined input aliases for a known game.")]
    public GameSpecificAlias? GameAliases = null;
}
