using TPP.Persistence.Models;

namespace TPP.Core
{
    public enum MessageSource
    {
        Chat,
        Whisper,
    }

    public record Message(
        User User,
        string MessageText,
        MessageSource MessageSource,
        string RawIrcMessage);
}
