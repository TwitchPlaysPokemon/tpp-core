using System.Collections.Generic;
using NodaTime;
using TPP.Common;

//public record Match(long Pokeyen, long Tokens, long PokeyenHighScore);

namespace TPP.Model;

public class User(
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
    Instant? lastWhisperReceivedAt = null,
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
    : PropertyEquatable<User>
{
    /// <summary>
    /// A user's ID, as delivered by Twitch. This is a user's identity (names can change!).
    /// </summary>
    public string Id { get; init; } = id;
    protected override object EqualityId => Id;

    /// <summary>
    /// Display name as advertised as by Twitch. Usually this just varies to the simple name in capitalization,
    /// but it may include unicode for some people, e.g. chinese, japanese or korean characters.
    /// </summary>
    public string TwitchDisplayName { get; init; } = twitchDisplayName;

    /// <summary>
    /// Ascii-compatible display name. Ideally this is identical to the Twitch display name,
    /// but we need a readable backup-name for the few people with unicode in their name.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// A simple, lowercased, ascii-compatible version of the user's name. Is used in IRC for example.
    /// </summary>
    public string SimpleName { get; init; } = simpleName;

    public string? Color { get; init; } = color;

    public Instant FirstActiveAt { get; init; } = firstActiveAt;
    public Instant LastActiveAt { get; init; } = lastActiveAt;
    // public Instant? FollowedAt { get; init; }
    // public Instant? LastBetAt { get; init; }
    public Instant? LastMessageAt { get; init; } = lastMessageAt;
    public Instant? LastWhisperReceivedAt { get; init; } = lastWhisperReceivedAt;

    public long Pokeyen { get; init; } = pokeyen;
    public long Tokens { get; init; } = tokens;
    public long PokeyenHighScore { get; init; } = pokeyenHighScore;

    public SortedSet<int> ParticipationEmblems { get; init; } = participationEmblems ?? [];
    public int? SelectedParticipationEmblem { get; init; } = selectedParticipationEmblem;

    public PkmnSpecies? SelectedBadge { get; init; } = selectedBadge;

    public string? GlowColor { get; init; } = glowColor;
    public bool GlowColorUnlocked { get; init; } = glowColorUnlocked;

    public int? PokeyenBetRank { get; init; } = pokeyenBetRank;

    public HashSet<Role> Roles { get; init; } = roles ?? [];

    // public bool Active { get; init; }
    // public bool Follower { get; init; }
    // public List<string> Badges { get; init; } // twitch badges, e.g. subscriber/24
    // public Dictionary<string, string> Milestones { get; init; }

    public bool IsSubscribed { get; init; } = isSubscribed;
    public int MonthsSubscribed { get; init; } = monthsSubscribed;
    public SubscriptionTier? SubscriptionTier { get; init; } = subscriptionTier;
    public int LoyaltyLeague { get; init; } = loyaltyLeague;
    public Instant? SubscriptionUpdatedAt { get; init; } = subscriptionUpdatedAt;
    // public int RankedPokeyen { get; init; } // wtf is this used for?
    // public int PreviousPokeyenBetRank { get; init; } // wtf is this used for?
    // public int PokeyenBetRankVersion { get; init; } // wtf is this used for?

    // TODO unlocked items?

    // public bool Imported { get; init; }

    public Instant? TimeoutExpiration { get; init; }
    public bool Banned { get; init; }
    public bool IsBot { get; init; }
    public bool IsCaptchaSuspended { get; init; }

    public bool DonorBadge { get; init; }

    public override string ToString() => $"User({Id}/{SimpleName})";
}
