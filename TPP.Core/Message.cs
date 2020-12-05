using TPP.Persistence.Models;

namespace TPP.Core
{
    public enum MessageSource
    {
        Chat,
        Whisper,
    }

    public sealed record MessageDetails(string? MessageId, bool IsAction, bool IsStaff);

    public sealed record Message(
        User User,
        string MessageText,
        MessageSource MessageSource,
        string RawIrcMessage)
    {
        public MessageDetails Details { get; init; } = new(MessageId: null, IsAction: false, IsStaff: false);
    }
}
