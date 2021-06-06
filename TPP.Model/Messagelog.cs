using NodaTime;

namespace TPP.Model
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
