using System;
using System.Collections.Generic;
using NodaTime;
using TPP.Common;

namespace TPP.Persistence.Models
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
}
