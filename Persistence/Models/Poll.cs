using Common;
using System;
using System.Collections.Generic;
using System.Text;
using NodaTime;

namespace Persistence.Models
{
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public class PollOption : PropertyEquatable<PollOption>
    {
        public int Id { get; private set; }
        public string? Options { get; private set; }
        public int Votes { get; private set; }
        public List<string>? VoterIds { get; private set; }

        protected override object EqualityId => Id;

        public PollOption(int id, string options, int votes, List<string> voterIds)
        {
            Id = id;
            Options = options;
            Votes = votes;
            VoterIds = voterIds;
        }
    }

    public class Poll : PropertyEquatable<Poll>
    {
        public string Id { get; private set; }
        protected override object EqualityId => Id;

        public string PollName { get; private set; }

        /// <summary>
        /// The subject this poll is about.
        /// </summary>
        public string PollCode { get; private set; }

        //public struct PollOption
        //{
        //    public int Id;
        //    public string Option;
        //    public int Votes;
        //    public List<string> VoterIds;
        //};

        public List<string> Voters { get; private set; }

        /// <summary>
        /// The list of options and their respective votes.
        /// </summary>
        public PollOption[] PollOptions { get; private set; }

        /// <summary>
        /// Instant this poll was created at.
        /// </summary>
        public Instant CreatedAt { get; private set; }

        /// <summary>
        /// Specifies whether the poll is allowed more than one choice. Limited to one if false.
        /// </summary>
        public bool MultiChoice { get; private set; }

        /// <summary>
        /// Sets if this poll is active. If false, will block any more voters.
        /// </summary>
        public bool Alive { get; private set; }

        public Poll(string id, string pollName, string pollCode, List<string> voters, PollOption[] pollOptions, Instant createdAt, bool multiChoice, bool alive)
        {
            Id = id;
            PollName = pollName;
            PollCode = pollCode;
            Voters = voters;
            PollOptions = pollOptions;
            CreatedAt = createdAt;
            MultiChoice = multiChoice;
            Alive = alive;
        }

    }
}
