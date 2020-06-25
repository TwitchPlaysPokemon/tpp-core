using System.Collections.Generic;
using Common;
using NodaTime;

namespace Persistence.Models
{
    public class User : PropertyEquatable<User>
    {
        /// <summary>
        /// A user's ID, as delivered by Twitch. This is a user's identity (names can change!).
        /// </summary>
        public string Id { get; init; }
        protected override object EqualityId => Id;

        /// <summary>
        /// Display name as advertised as by Twitch. Usually this just varies to the simple name in capitalization,
        /// but it may include unicode for some people, e.g. chinese, japanese or korean characters.
        /// </summary>
        public string TwitchDisplayName { get; init; }

        /// <summary>
        /// Ascii-compatible display name. Ideally this is identical to the Twitch display name,
        /// but we need a readable backup-name for the few people with unicode in their name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// A simple, lowercased, ascii-compatible version of the user's name. Is used in IRC for example.
        /// </summary>
        public string SimpleName { get; init; }

        public string? Color { get; init; }

        public Instant FirstActiveAt { get; init; }
        public Instant LastActiveAt { get; init; }
        // public Instant? FollowedAt { get; init; }
        // public Instant? SubscriptionUpdatedAt { get; init; }
        // public Instant? LastBetAt { get; init; }
        public Instant? LastMessageAt { get; init; }

        public int Pokeyen { get; init; }
        public int Tokens { get; init; }

        public SortedSet<int> ParticipationEmblems { get; init; } = new SortedSet<int>();
        public int? SelectedParticipationEmblem { get; init; } = null;

        public PkmnSpecies? SelectedBadge { get; init; } = null;

        // public string? SecondaryColor { get; init; }
        // public bool SecondaryColorUnlocked { get; init; }
        // public bool Active { get; init; }
        // public bool Follower { get; init; }
        // public bool Subscriber { get; init; }
        // public bool Turbo { get; init; }
        // public List<string> Badges { get; init; } // twitch badges, e.g. subscriber/24
        // public Dictionary<string, string> Milestones { get; init; }

        // public int MonthsSubscribed { get; init; }
        // public int RankedPokeyen { get; init; } // wtf is this used for?
        // public int PreviousPokeyenBetRank { get; init; } // wtf is this used for?
        // public int PokeyenBetRankVersion { get; init; } // wtf is this used for?

        // public int LoyaltyTier { get; init; }
        // TODO unlocked items?

        // public bool Imported { get; init; }

        public User(
            string id,
            string twitchDisplayName,
            string name,
            string simpleName,
            string? color,
            Instant firstActiveAt,
            Instant lastActiveAt,
            Instant? lastMessageAt,
            int pokeyen,
            int tokens)
        {
            Id = id;
            TwitchDisplayName = twitchDisplayName;
            Name = name;
            SimpleName = simpleName;
            Color = color;
            FirstActiveAt = firstActiveAt;
            LastActiveAt = lastActiveAt;
            LastMessageAt = lastMessageAt;
            Pokeyen = pokeyen;
            Tokens = tokens;
        }

        public override string ToString() => $"User({Id}/{SimpleName})";
    }
}
