using NodaTime;

namespace TPP.Model
{
    public record ModLog(string Id, string UserId, string Reason, string Rule, Instant Timestamp);
}
