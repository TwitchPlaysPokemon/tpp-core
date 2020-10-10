using System.Collections.Generic;
using Common;
using NodaTime;
using Persistence.Repos;

namespace Persistence.Models
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
        public int OldBalance { get; init; }
        public int NewBalance { get; init; }
        public int Change { get; init; }

        public Instant CreatedAt { get; init; }

        public string? Type { get; init; }

        public IDictionary<string, object?> AdditionalData { get; init; }

        public TransactionLog(
            string id,
            string userId,
            int oldBalance,
            int newBalance,
            int change,
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
