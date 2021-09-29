using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using TPP.Inputting;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public sealed class NewAnarchyInput : IOverlayEvent
{
    public string OverlayEventType => "new_anarchy_input";

    [DataMember(Name = "button_set")] public IImmutableList<string> ButtonSet { get; set; }
    [DataMember(Name = "button_set_labels")] public IImmutableList<string> ButtonSetLabels { get; set; }
    [DataMember(Name = "user")] public User User { get; set; }
    [DataMember(Name = "id")] public int InputId { get; set; }

    public NewAnarchyInput(int inputId, InputSet inputSet, User user)
    {
        ButtonSet = inputSet.Inputs.Select(i => i.ButtonName).ToImmutableList();
        ButtonSetLabels = inputSet.Inputs.Select(i => i.DisplayedText).ToImmutableList();
        User = user;
        InputId = inputId;
    }
}

[DataContract]
public sealed class AnarchyInputStart : IOverlayEvent
{
    public string OverlayEventType => "anarchy_input_start";

    [DataMember(Name = "button_set")] public IImmutableList<string> ButtonSet { get; set; }
    [DataMember(Name = "id")] public int InputId { get; set; }
    [DataMember(Name = "frames")] public int HeldFrames { get; set; }
    [DataMember(Name = "sleep_frames")] public int SleepFrames { get; set; }

    public AnarchyInputStart(int inputId, TimedInputSet timedInputSet, float fps)
    {
        ButtonSet = timedInputSet.InputSet.Inputs.Select(i => i.ButtonName).ToImmutableList();
        InputId = inputId;
        HeldFrames = (int)Math.Round(timedInputSet.HoldDuration * fps);
        SleepFrames = (int)Math.Round(timedInputSet.SleepDuration * fps);
    }
}

[DataContract]
public sealed class AnarchyInputStop : IOverlayEvent
{
    public string OverlayEventType => "anarchy_input_stop";

    [DataMember(Name = "id")] public int InputId { get; set; }
    public AnarchyInputStop(int inputId) => InputId = inputId;
}
