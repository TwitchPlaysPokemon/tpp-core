using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using TPP.Inputting;
using TPP.Inputting.Inputs;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public sealed class NewAnarchyInput : IOverlayEvent
{
    public string OverlayEventType => "new_anarchy_input";

    [DataMember(Name = "button_set")] public IImmutableList<string> ButtonSet { get; set; }
    [DataMember(Name = "button_set_labels")] public IImmutableList<string> ButtonSetLabels { get; set; }
    [DataMember(Name = "button_set_velocities")] public IImmutableList<float> ButtonSetVelocities { get; set; }
    [DataMember(Name = "user")] public User User { get; set; }
    [DataMember(Name = "id")] public long InputId { get; set; }
    [DataMember(Name = "x", EmitDefaultValue = false)] public uint? X { get; set; }
    [DataMember(Name = "y", EmitDefaultValue = false)] public uint? Y { get; set; }
    [DataMember(Name = "x2", EmitDefaultValue = false)] public uint? X2 { get; set; }
    [DataMember(Name = "y2", EmitDefaultValue = false)] public uint? Y2 { get; set; }
    [DataMember(Name = "side", EmitDefaultValue = false)] public string? Side { get; set; }
    [DataMember(Name = "direct", EmitDefaultValue = false)] public bool? Direct { get; set; }
    [DataMember(Name = "channel")] public string? Channel { get; set; }
    [DataMember(Name = "channel_image_url")] public string? ChannelImageUrl { get; set; }

    public NewAnarchyInput(long inputId, InputSet inputSet, User user, string? channel, string? channelImageUrl)
    {
        IEnumerable<Input> InputsForOverlay() => inputSet.Inputs.Where(i => i is not SideInput);
        ButtonSet = InputsForOverlay().Select(i => i.ButtonName).ToImmutableList();
        ButtonSetLabels = InputsForOverlay().Select(i => i.DisplayedText).ToImmutableList();
        ButtonSetVelocities = InputsForOverlay().Select(i => i is AnalogInput analog ? analog.Strength : 1.0f).ToImmutableList();
        User = user;
        InputId = inputId;
        SideInput? sideInput = inputSet.Inputs.Where(i => i is SideInput).Cast<SideInput>().FirstOrDefault();
        Side = (sideInput?.Side)?.GetSideString();
        Direct = sideInput?.Direct;
        Channel = channel;
        ChannelImageUrl = channelImageUrl;
        var touchInput = (TouchscreenInput?)InputsForOverlay().FirstOrDefault(input => input is TouchscreenInput);
        var dragInput = (TouchscreenDragInput?)InputsForOverlay().FirstOrDefault(input => input is TouchscreenDragInput);
        if (touchInput != null)
        {
            X = touchInput.X;
            Y = touchInput.Y;
        }
        if (dragInput != null)
        {
            X = dragInput.X;
            Y = dragInput.Y;
            X2 = dragInput.X2;
            Y2 = dragInput.Y2;
        }
    }
}

[DataContract]
public sealed class AnarchyInputStart : IOverlayEvent
{
    public string OverlayEventType => "anarchy_input_start";

    [DataMember(Name = "button_set")] public IImmutableList<string> ButtonSet { get; set; }
    [DataMember(Name = "id")] public long InputId { get; set; }
    [DataMember(Name = "frames")] public int HeldFrames { get; set; }
    [DataMember(Name = "sleep_frames")] public int SleepFrames { get; set; }

    public AnarchyInputStart(long inputId, TimedInputSet timedInputSet, float fps)
    {
        ButtonSet = timedInputSet.InputSet.Inputs.Select(i => i.ButtonName).ToImmutableList();
        InputId = inputId;
        HeldFrames = (int)Math.Round(timedInputSet.HoldDuration * fps);
        SleepFrames = (int)Math.Round(timedInputSet.SleepDuration * fps);
    }
}

[DataContract]
public sealed class AnarchyInputStop(long inputId) : IOverlayEvent
{
    public string OverlayEventType => "anarchy_input_stop";

    [DataMember(Name = "id")] public long InputId { get; set; } = inputId;
}

[DataContract]
public sealed class ButtonPressUpdate(long numTotalButtonPresses) : IOverlayEvent
{
    public string OverlayEventType => "button_press_update";

    [DataMember(Name = "presses")] public long NumTotalButtonPresses { get; set; } = numTotalButtonPresses;
}
