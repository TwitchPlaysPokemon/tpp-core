using System.Collections.Generic;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Repos;

namespace TPP.Persistence.Models
{
    /// <summary>
    /// Transaction logs are read-only entities that get created by <see cref="IBank{T}"/> implementations.
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
}
