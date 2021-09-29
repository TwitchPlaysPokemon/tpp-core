namespace TPP.Core.Configuration;

/// Contains all parameters that affect how inputs are performed.
public sealed class InputConfig : ConfigBase
{
    public ButtonProfile ButtonsProfile { get; init; } = ButtonProfile.GameBoy;
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
}
