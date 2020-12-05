using NodaTime;

namespace Persistence.Models
{
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
}
