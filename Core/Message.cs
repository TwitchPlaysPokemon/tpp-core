using Persistence.Models;

namespace Core
{
    public class Message
    {
        public User User { get; }
        public string MessageText { get; }

        public Message(User user, string messageText)
        {
            User = user;
            MessageText = messageText;
        }
    }
}
