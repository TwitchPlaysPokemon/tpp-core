using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Persistence.Models
{
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public class PollOption : PropertyEquatable<PollOption>
    {
        public int Id { get; init; }
        public string? Option { get; init; }
        public int Votes { get; init; }
        public List<string>? VoterIds { get; init; }

        protected override object EqualityId => Id;

        public PollOption(int id, string option, int votes, List<string> voterIds)
        {
            Id = id;
            Option = option;
            Votes = votes;
            VoterIds = voterIds;
        }
    }

    public class Poll : PropertyEquatable<Poll>
    {
        public string Id { get; init; }
        protected override object EqualityId => Id;

        /// <summary>
        /// The subject this poll is about.
        /// </summary>
        public string PollTitle { get; init; }

        /// <summary>
        /// The capitalized 4-letter code used to publicly identify this poll.
        /// </summary>
        public string PollCode { get; init; }

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

        public Poll(string id, string pollTitle, string pollCode, List<string> voters, List<PollOption> pollOptions, Instant createdAt, bool multiChoice, bool alive)
        {
            Id = id;
            PollTitle = pollTitle;
            PollCode = pollCode;
            Voters = voters;
            PollOptions = pollOptions;
            CreatedAt = createdAt;
            MultiChoice = multiChoice;
            Alive = alive;
        }

    }
}
