using System.Collections.Immutable;
using NodaTime;

namespace Persistence.Models
{
    public record CommandLog(
        string Id,
        string UserId,
        string Command,
        ImmutableList<string> Args,
        Instant Timestamp,
        string? Response);
}
