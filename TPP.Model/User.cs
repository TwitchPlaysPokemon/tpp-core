using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Model;

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
    // public Instant? LastBetAt { get; init; }
    public Instant? LastMessageAt { get; init; }

    public long Pokeyen { get; init; }
    public long Tokens { get; init; }
    public long PokeyenHighScore { get; init; }

    public SortedSet<int> ParticipationEmblems { get; init; }
    public int? SelectedParticipationEmblem { get; init; }

    public PkmnSpecies? SelectedBadge { get; init; }

    public string? GlowColor { get; init; }
    public bool GlowColorUnlocked { get; init; }

    public int? PokeyenBetRank { get; init; }

    public HashSet<Role> Roles { get; init; }

    // public bool Active { get; init; }
    // public bool Follower { get; init; }
    // public List<string> Badges { get; init; } // twitch badges, e.g. subscriber/24
    // public Dictionary<string, string> Milestones { get; init; }

    public bool IsSubscribed { get; init; }
    public int MonthsSubscribed { get; init; }
    public SubscriptionTier? SubscriptionTier { get; init; }
    public int LoyaltyLeague { get; init; }
    public Instant? SubscriptionUpdatedAt { get; init; }
    // public int RankedPokeyen { get; init; } // wtf is this used for?
    // public int PreviousPokeyenBetRank { get; init; } // wtf is this used for?
    // public int PokeyenBetRankVersion { get; init; } // wtf is this used for?

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
        long pokeyenHighScore = 0,
        SortedSet<int>? participationEmblems = null,
        int? selectedParticipationEmblem = null,
        PkmnSpecies? selectedBadge = null,
        string? glowColor = null,
        bool glowColorUnlocked = false,
        int? pokeyenBetRank = null,
        HashSet<Role>? roles = null,
        bool isSubscribed = false,
        int monthsSubscribed = 0,
        SubscriptionTier? subscriptionTier = null,
        int loyaltyLeague = 0,
        Instant? subscriptionUpdatedAt = null)
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
        PokeyenHighScore = pokeyenHighScore;
        ParticipationEmblems = participationEmblems ?? new SortedSet<int>();
        SelectedParticipationEmblem = selectedParticipationEmblem;
        SelectedBadge = selectedBadge;
        GlowColor = glowColor;
        GlowColorUnlocked = glowColorUnlocked;
        PokeyenBetRank = pokeyenBetRank;
        IsSubscribed = isSubscribed;
        MonthsSubscribed = monthsSubscribed;
        SubscriptionTier = subscriptionTier;
        LoyaltyLeague = loyaltyLeague;
        SubscriptionUpdatedAt = subscriptionUpdatedAt;
        Roles = roles ?? new HashSet<Role>();
    }

    public override string ToString() => $"User({Id}/{SimpleName})";
}
