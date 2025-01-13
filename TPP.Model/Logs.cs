using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using TPP.Common;

namespace TPP.Model;

/// <summary>
/// All badges are unique objects in the database, and every time some badge mutates, that gets logged.
/// </summary>
/// <param name="Id">The log entry's primary key</param>
/// <param name="BadgeId">The affected badge's primary key</param>
/// <param name="BadgeLogType">What kind of event happened to the badge.
/// A non-exhaustive list can be found at TPP.Persistence.BadgeLogType</param>
/// <param name="UserId">The user ID of the badge's owner. For transfers, this is the recipient.
/// Can be null in case the badge was consumed, and hence transferred to "noone".</param>
/// <param name="OldUserId">The user ID of the badge's previous owner, for transfers.
/// If null, this doesn't necessarily mean that the badge didn't have an owner previously,
/// but that the previous owner just isn't logged here. There are two typical reasons:
/// 1) It's not an event that changes ownership, or
/// 2) it wasn't logged at all before 2016-12-03 (see also BadgeLogRepo.CorrectOwnershipTrackingSince)
/// But it _can_ mean that there was no previous owner, e.g. for transmutation_rollback.
/// </param>
/// <param name="Timestamp">When the event happened</param>
/// <param name="AdditionalData">Any event-type-specific data. Note that </param>
public sealed record BadgeLog(
    string Id,
    string BadgeId,
    string BadgeLogType,
    string? UserId,
    string? OldUserId,
    Instant Timestamp,
    IDictionary<string, object?> AdditionalData)
{
    public bool Equals(BadgeLog? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
               && BadgeId == other.BadgeId
               && BadgeLogType == other.BadgeLogType
               && UserId == other.UserId
               && OldUserId == other.OldUserId
               && Timestamp.Equals(other.Timestamp)
               && AdditionalData.DictionaryEqual(other.AdditionalData); // <-- Equals() is overridden just for this
    }

    public override int GetHashCode() =>
        HashCode.Combine(Id, BadgeId, BadgeLogType, UserId, Timestamp, AdditionalData);
}

public sealed record CommandLog(
    string Id,
    string UserId,
    string Command,
    IReadOnlyList<string> Args,
    Instant Timestamp,
    string? Response)
{
    public bool Equals(CommandLog? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
               && UserId == other.UserId
               && Command == other.Command
               && Args.SequenceEqual(other.Args) // <-- Equals() is overridden just for this
               && Timestamp.Equals(other.Timestamp)
               && Response == other.Response;
    }

    public override int GetHashCode() => HashCode.Combine(Id, UserId, Command, Args, Timestamp, Response);
}

/// <summary>
/// Raw log entry of an incoming message.
/// For logging purposes only.
/// </summary>
public record Messagelog(
    string Id,
    string IrcLine,
    string UserId,
    string Message,
    Instant Timestamp);

public record ModbotLog(string Id, string UserId, string Reason, string Rule, Instant Timestamp);

public record BanLog(
    string Id,
    string Type,
    string UserId,
    string Reason,
    string? IssuerUserId,
    Instant Timestamp);

public record TimeoutLog(
    string Id,
    string Type,
    string UserId,
    string Reason,
    string? IssuerUserId,
    Instant Timestamp,
    Duration? Duration);

public record SubscriptionLog(
    string Id,
    string UserId,
    Instant Timestamp,
    int? MonthsStreak,
    int MonthsNumPrev,
    int MonthsNumNew,
    int MonthsDifference,
    int LoyaltyLeaguePrev,
    int LoyaltyLeagueNew,
    int LoyaltyCompletions,
    int RewardTokens,
    bool IsGift,
    string? SubMessage,
    SubscriptionTier SubPlan,
    string? SubPlanName);

/// <summary>
/// Transaction logs are read-only entities that get created by <c>IBank&lt;T&gt;</c> implementations.
/// They are purely for traceability and serve no functional purpose.
/// </summary>
public class TransactionLog(
    string id,
    string userId,
    long oldBalance,
    long newBalance,
    long change,
    Instant createdAt,
    string? type,
    IDictionary<string, object?> additionalData)
    : PropertyEquatable<TransactionLog>
{
    public string Id { get; init; } = id;
    protected override object EqualityId => Id;

    public string UserId { get; init; } = userId;
    public long OldBalance { get; init; } = oldBalance;
    public long NewBalance { get; init; } = newBalance;
    public long Change { get; init; } = change;

    public Instant CreatedAt { get; init; } = createdAt;

    public string? Type { get; init; } = type;

    public IDictionary<string, object?> AdditionalData { get; init; } = additionalData;
}

public record InputLog(string Id, string UserId, string Message, Instant Timestamp);

public record TransmutationLog(
    string Id,
    string UserId,
    Instant Timestamp,
    int Cost,
    IReadOnlyList<string> InputBadges,
    string OutputBadge)
{
    public virtual bool Equals(TransmutationLog? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
               && UserId == other.UserId
               && Timestamp.Equals(other.Timestamp)
               && Cost == other.Cost
               && InputBadges.SequenceEqual(other.InputBadges) // <-- Equals() is overridden just for this
               && OutputBadge == other.OutputBadge;
    }
    public override int GetHashCode() =>
        HashCode.Combine(Id, UserId, Timestamp, Cost, InputBadges, OutputBadge);
}

public record ChattersSnapshot(
    string Id,
    IReadOnlyList<string> ChatterNames,
    IReadOnlyList<string> ChatterIds,
    Instant Timestamp,
    string Channel)
{
    public virtual bool Equals(ChattersSnapshot? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
               && ChatterNames.SequenceEqual(other.ChatterNames) // <-- Equals() is overridden just for this
               && ChatterIds.SequenceEqual(other.ChatterIds) // <-- Equals() is overridden just for this
               && Timestamp.Equals(other.Timestamp)
               && Channel == other.Channel;
    }
    public override int GetHashCode() =>
        HashCode.Combine(Id, ChatterNames, ChatterIds, Timestamp, Channel);
}
