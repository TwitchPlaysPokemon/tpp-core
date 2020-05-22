using System;
using System.Collections.Generic;
using Common;
using Persistence.Repos;

namespace Persistence.Models
{
    /// <summary>
    /// Transaction logs are read-only entities that get created by <see cref="IBank{T}"/> implementations.
    /// They are purely for traceability and serve no functional purpose.
    /// </summary>
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public class TransactionLog : PropertyEquatable<TransactionLog>
    {
        public string Id { get; private set; }
        protected override object EqualityId => Id;

        public string UserId { get; private set; }
        public int OldBalance { get; private set; }
        public int NewBalance { get; private set; }
        public int Change { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public string? Type { get; private set; }

        public IDictionary<string, object?> AdditionalData { get; private set; }

        public TransactionLog(
            string id,
            string userId,
            int oldBalance,
            int newBalance,
            int change,
            DateTime createdAt,
            string type,
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
