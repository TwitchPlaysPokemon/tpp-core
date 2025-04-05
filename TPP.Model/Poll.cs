using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Model;

// properties need setters for deserialization
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
public class PollOption(int id, string option, List<string> voterIds) : PropertyEquatable<PollOption>
{
    public int Id { get; init; } = id;
    public string? Option { get; init; } = option;
    public List<string> VoterIds { get; init; } = voterIds;

    protected override object EqualityId => Id;
}

public class Poll(
    string pollCode,
    string pollTitle,
    List<string> voters,
    List<PollOption> pollOptions,
    Instant createdAt,
    bool multiChoice,
    bool alive,
    bool allowChangeVote)
    : PropertyEquatable<Poll>
{
    /// <summary>
    /// The code used to publicly identify this poll.
    /// </summary>
    public string PollCode { get; init; } = pollCode;
    protected override object EqualityId => PollCode;

    /// <summary>
    /// The subject this poll is about.
    /// </summary>
    public string PollTitle { get; init; } = pollTitle;

    public List<string> Voters { get; init; } = voters;

    /// <summary>
    /// The list of options and their respective votes.
    /// </summary>
    public List<PollOption> PollOptions { get; init; } = pollOptions;

    public Instant CreatedAt { get; init; } = createdAt;

    /// <summary>
    /// Specifies whether the poll is allowed more than one choice. Limited to one if false.
    /// </summary>
    public bool MultiChoice { get; init; } = multiChoice;

    /// <summary>
    /// Sets if this poll is active. If false, will block any more voters.
    /// </summary>
    public bool Alive { get; init; } = alive;

    /// <summary>
    /// Whether users can change their vote. Otherwise they cannot change their once they voted once.
    /// </summary>
    public bool AllowChangeVote { get; init; } = allowChangeVote;
}
