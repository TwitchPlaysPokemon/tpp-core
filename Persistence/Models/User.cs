using System.Collections.Generic;
using Common;
using NodaTime;

namespace Persistence.Models
{
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public class User : PropertyEquatable<User>
    {
        /// <summary>
        /// A user's ID, as delivered by Twitch. This is a user's identity (names can change!).
        /// </summary>
        public string Id { get; private set; }
        protected override object EqualityId => Id;

        /// <summary>
        /// Display name as advertised as by Twitch. Usually this just varies to the simple name in capitalization,
        /// but it may include unicode for some people, e.g. chinese, japanese or korean characters.
        /// </summary>
        public string TwitchDisplayName { get; private set; }

        /// <summary>
        /// Ascii-compatible display name. Ideally this is identical to the Twitch display name,
        /// but we need a readable backup-name for the few people with unicode in their name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A simple, lowercased, ascii-compatible version of the user's name. Is used in IRC for example.
        /// </summary>
        public string SimpleName { get; private set; }

        public string? Color { get; private set; }

        public Instant FirstActiveAt { get; private set; }
        public Instant LastActiveAt { get; private set; }
        // public Instant? FollowedAt { get; private set; }
        // public Instant? SubscriptionUpdatedAt { get; private set; }
        // public Instant? LastBetAt { get; private set; }
        public Instant? LastMessageAt { get; private set; }

        public int Pokeyen { get; private set; }
        public int Tokens { get; private set; }

        public SortedSet<int> ParticipationEmblems { get; private set; }
        public int? SelectedParticipationEmblem { get; private set; }

        public PkmnSpecies? SelectedBadge { get; private set; }

        public string? GlowColor { get; private set; }
        public bool GlowColorUnlocked { get; private set; }

        public int? PokeyenBetRank { get; private set; }

        // public bool Active { get; private set; }
        // public bool Follower { get; private set; }
        // public bool Subscriber { get; private set; }
        // public bool Turbo { get; private set; }
        // public List<string> Badges { get; private set; } // twitch badges, e.g. subscriber/24
        // public Dictionary<string, string> Milestones { get; private set; }

        // public int MonthsSubscribed { get; private set; }
        // public int RankedPokeyen { get; private set; } // wtf is this used for?
        // public int PreviousPokeyenBetRank { get; private set; } // wtf is this used for?
        // public int PokeyenBetRankVersion { get; private set; } // wtf is this used for?

        // public int LoyaltyTier { get; private set; }
        // TODO unlocked items?

        // public bool Imported { get; private set; }

        public User(
            string id,
            string name,
            string twitchDisplayName,
            string simpleName,
            string? color,
            Instant firstActiveAt,
            Instant lastActiveAt,
            Instant? lastMessageAt,
            int pokeyen,
            int tokens,
            SortedSet<int>? participationEmblems = null,
            int? selectedParticipationEmblem = null,
            PkmnSpecies? selectedBadge = null,
            string? glowColor = null,
            bool glowColorUnlocked = false,
            int? pokeyenBetRank = null)
        {
            Id = id;
            Name = name;
            TwitchDisplayName = twitchDisplayName;
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
