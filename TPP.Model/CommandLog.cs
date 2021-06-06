using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace TPP.Persistence.Models
{
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
}
