using Persistence.Models;

namespace Core
{
    public enum MessageSource
    {
        Chat,
        Whisper,
    }

    public class Message
    {
        public User User { get; }
        public string MessageText { get; }
        public MessageSource MessageSource { get; }

        public Message(User user, string messageText, MessageSource messageSource)
        {
            User = user;
            MessageText = messageText;
            MessageSource = messageSource;
        }
    }
}
