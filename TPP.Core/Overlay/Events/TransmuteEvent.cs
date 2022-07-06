using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using TPP.Common;

namespace TPP.Core.Overlay.Events;

[DataContract]
public class TransmuteEvent : IOverlayEvent
{
    public string OverlayEventType => "new_transmutation";

    [DataMember(Name = "username")] public string Username { get; set; }
    [DataMember(Name = "inputs")] public IImmutableList<string> Inputs { get; set; }
    [DataMember(Name = "output")] public string Output { get; set; }
    [DataMember(Name = "output_candidates")] public IImmutableList<string> OutputCandidates { get; set; }
    [DataMember(Name = "duration")] public int DurationSeconds { get; set; }

    public TransmuteEvent(
        string username,
        IEnumerable<PkmnSpecies> inputs,
        PkmnSpecies output,
        IEnumerable<PkmnSpecies> outputCandidates,
        int durationSeconds = 15)
    {
        Username = username;
        Inputs = inputs.Select(s => s.Id).ToImmutableList();
        Output = output.Id;
        OutputCandidates = outputCandidates.Select(s => s.Id).ToImmutableList();
        DurationSeconds = durationSeconds;
    }
}
