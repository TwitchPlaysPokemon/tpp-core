using NodaTime;

namespace TPP.Persistence.Models
{
    public record ModLog(string Id, string UserId, string Reason, string Rule, Instant Timestamp);
}
