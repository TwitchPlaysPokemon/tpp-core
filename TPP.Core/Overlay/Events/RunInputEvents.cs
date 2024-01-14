using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using NodaTime;
using TPP.Inputting;
using TPP.Inputting.Inputs;
using TPP.Model;

namespace TPP.Core.Overlay.Events
{
    [DataContract]
    public sealed class NewAnarchyInput : IOverlayEvent
    {
        public string OverlayEventType => "new_anarchy_input";

        [DataMember(Name = "button_set")] public IImmutableList<string> ButtonSet { get; set; }
        [DataMember(Name = "button_set_labels")] public IImmutableList<string> ButtonSetLabels { get; set; }
        [DataMember(Name = "button_set_velocities")] public IImmutableList<float> ButtonSetVelocities { get; set; }
        [DataMember(Name = "user")] public User User { get; set; }
        [DataMember(Name = "id")] public int InputId { get; set; }
        [DataMember(Name = "side", EmitDefaultValue = false)] public string? Side { get; set; }
        [DataMember(Name = "direct", EmitDefaultValue = false)] public bool? Direct { get; set; }

        public NewAnarchyInput(int inputId, InputSet inputSet, User user)
        {
            IEnumerable<Input> InputsForOverlay() => inputSet.Inputs.Where(i => i is not SideInput);
            ButtonSet = InputsForOverlay().Select(i => i.ButtonName).ToImmutableList();
            ButtonSetLabels = InputsForOverlay().Select(i => i.DisplayedText).ToImmutableList();
            // velocities: was used for analog, but probably needs a rework anyway. Just use dummy values for now.
            ButtonSetVelocities = ButtonSet.Select(_ => 1.0f).ToImmutableList();
            User = user;
            InputId = inputId;
            SideInput? sideInput = inputSet.Inputs.Where(i => i is SideInput).Cast<SideInput>().FirstOrDefault();
            Side = (sideInput?.Side)?.GetSideString();
            Direct = sideInput?.Direct;
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

    [DataContract]
    public sealed class ButtonPressesCountUpdate : IOverlayEvent
    {
        public string OverlayEventType => "button_press_update";

        [DataMember(Name = "presses")] public long NumTotalButtonPresses { get; set; }
        public ButtonPressesCountUpdate(long numTotalButtonPresses) => NumTotalButtonPresses = numTotalButtonPresses;
    }

    [DataContract]
    public readonly struct NewVote
    {
        [DataMember(Name = "command")] public string Command { get; init; }
        [DataMember(Name = "user")] public User User { get; init; }
        // [DataMember(Name = "button_sequence")] public InputSequence InputSequence { get; init; } // unused
        // [DataMember(Name = "ts")] public Instant Timestamp { get; init; } // unused
    }

    [DataContract]
    public readonly struct Vote
    {
        [DataMember(Name = "command")] public string Command { get; init; }
        [DataMember(Name = "count")] public int Count { get; init; }
        // [DataMember(Name = "button_sequence")] public InputSequence InputSequence { get; init; } // unused
    }

    [DataContract]
    public sealed class DemocracyVotesUpdate : IOverlayEvent
    {
        public string OverlayEventType => "democracy_new_vote";

        [DataMember(Name = "new_vote")] public NewVote NewVote { get; init; }
        [DataMember(Name = "votes")] public List<Vote> Votes { get; init; }

        public DemocracyVotesUpdate(
            User newVoteUser,
            InputSequence votedInput,
            IReadOnlyDictionary<InputSequence, int> votes)
        {
            NewVote = new NewVote { User = newVoteUser, Command = votedInput.OriginalText };
            Votes = votes
                .Select(kvp => new Vote { Command = kvp.Key.OriginalText, Count = kvp.Value })
                .OrderBy(vote => vote.Count)
                .ToList();
        }
    }

    [DataContract]
    public sealed class DemocracyReset : IOverlayEvent
    {
        public string OverlayEventType => "democracy_reset";

        [DataMember(Name = "vote_ends_at")] public Instant Timestamp { get; init; }
        public DemocracyReset(Instant timestamp) => Timestamp = timestamp;
    }

    [DataContract]
    public sealed class DemocracyVotingOver : IOverlayEvent
    {
        public string OverlayEventType => "democracy_voting_over";

        [DataMember(Name = "winning_button_sequence")] public string WinningSequence { get; init; }
        public DemocracyVotingOver(InputSequence input) => WinningSequence = input.OriginalText;
    }

    [DataContract]
    public sealed class DemocracySequenceStart : IOverlayEvent
    {
        public string OverlayEventType => "democracy_sequence_start";

        [DataMember(Name = "button_sequence")] public List<List<string>> Sequence { get; init; }
        public DemocracySequenceStart(InputSequence input)
        {
            Sequence = input.InputSets
                .Select(set => set.Inputs
                    .Select(i => i.DisplayedText).ToList()).ToList();
        }
    }
}
