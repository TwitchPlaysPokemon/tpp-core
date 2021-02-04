using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Persistence.Models
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

        public long Pokeyen { get; init; }
        public long Tokens { get; init; }

        public SortedSet<int> ParticipationEmblems { get; init; }
        public int? SelectedParticipationEmblem { get; init; }

        public PkmnSpecies? SelectedBadge { get; init; }

        public string? GlowColor { get; init; }
        public bool GlowColorUnlocked { get; init; }

        public int? PokeyenBetRank { get; init; }

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
            long pokeyen,
            long tokens,
            SortedSet<int>? participationEmblems = null,
            int? selectedParticipationEmblem = null,
            PkmnSpecies? selectedBadge = null,
            string? glowColor = null,
            bool glowColorUnlocked = false,
            int? pokeyenBetRank = null)
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
            ParticipationEmblems = participationEmblems ?? new SortedSet<int>();
            SelectedParticipationEmblem = selectedParticipationEmblem;
            SelectedBadge = selectedBadge;
            GlowColor = glowColor;
            GlowColorUnlocked = glowColorUnlocked;
            PokeyenBetRank = pokeyenBetRank;
        }

        public override string ToString() => $"User({Id}/{SimpleName})";
    }
}
