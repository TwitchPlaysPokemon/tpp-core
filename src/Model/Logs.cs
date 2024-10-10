using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Common;

namespace Model
{
    public sealed record BadgeLog(
        string Id,
        string BadgeId,
        string BadgeLogType,
        string? UserId,
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
        string Id, string Type, string UserId, string Reason, string? IssuerUserId, Instant Timestamp);
    public record TimeoutLog(
        string Id, string Type, string UserId, string Reason, string? IssuerUserId, Instant Timestamp,
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
    public class TransactionLog : PropertyEquatable<TransactionLog>
    {
        public string Id { get; init; }
        protected override object EqualityId => Id;

        public string UserId { get; init; }
        public long OldBalance { get; init; }
        public long NewBalance { get; init; }
        public long Change { get; init; }

        public Instant CreatedAt { get; init; }

        public string? Type { get; init; }

        public IDictionary<string, object?> AdditionalData { get; init; }

        public TransactionLog(
            string id,
            string userId,
            long oldBalance,
            long newBalance,
            long change,
            Instant createdAt,
            string? type,
            IDictionary<string, object?> additionalData)
        {
            Id = id;
            UserId = userId;
            OldBalance = oldBalance;
            NewBalance = newBalance;
            Change = change;
            CreatedAt = createdAt;
            Type = type;
            AdditionalData = additionalData;
        }
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

}
