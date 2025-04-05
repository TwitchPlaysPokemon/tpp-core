using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Model;

// properties need setters for deserialization
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
public class PollOption : PropertyEquatable<PollOption>
{
    public int Id { get; init; }
    public string? Option { get; init; }
    public List<string> VoterIds { get; init; }

    protected override object EqualityId => Id;

    public PollOption(int id, string option, List<string> voterIds)
    {
        Id = id;
        Option = option;
        VoterIds = voterIds;
    }
}

public class Poll : PropertyEquatable<Poll>
{
    /// <summary>
    /// The code used to publicly identify this poll.
    /// </summary>
    public string PollCode { get; init; }
    protected override object EqualityId => PollCode;

    /// <summary>
    /// The subject this poll is about.
    /// </summary>
    public string PollTitle { get; init; }

    public List<string> Voters { get; init; }

    /// <summary>
    /// The list of options and their respective votes.
    /// </summary>
    public List<PollOption> PollOptions { get; init; }

    public Instant CreatedAt { get; init; }

    /// <summary>
    /// Specifies whether the poll is allowed more than one choice. Limited to one if false.
    /// </summary>
    public bool MultiChoice { get; init; }

    /// <summary>
    /// Sets if this poll is active. If false, will block any more voters.
    /// </summary>
    public bool Alive { get; init; }

    /// <summary>
    /// Whether users can change their vote. Otherwise they cannot change their once they voted once.
    /// </summary>
    public bool AllowChangeVote { get; init; }

    public Poll(
        string pollCode, string pollTitle, List<string> voters, List<PollOption> pollOptions,
        Instant createdAt, bool multiChoice, bool alive, bool allowChangeVote)
    {
        PollCode = pollCode;
        PollTitle = pollTitle;
        Voters = voters;
        PollOptions = pollOptions;
        CreatedAt = createdAt;
        MultiChoice = multiChoice;
        Alive = alive;
        AllowChangeVote = allowChangeVote;
    }

}
