using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TPPCore.Service.Example.Parrot
{
    public class Model
    {
        private const int maxMessages = 100;

        public ReadOnlyCollection<string> RecentMessages
        {
            get { return recentMessages.AsReadOnly(); }
        }

        public string CurrentMessage
        {
            get { return currentMessage; }
        }

        public int RepeatCount
        {
            get { return repeatCount; }
        }

        private List<string> recentMessages;
        private string currentMessage = "hello world!";
        private int repeatCount = 0;

        public Model()
        {
            recentMessages = new List<string>();
        }

        public void Repeat()
        {
            repeatCount += 1;
            recentMessages.Add(currentMessage);
            removeOldMessages();
        }

        public void RepeatNewMessage(string message)
        {
            if (message != currentMessage)
            {
                currentMessage = message;
                repeatCount = 0;
            }

            recentMessages.Add(message);
            removeOldMessages();
        }

        private void removeOldMessages()
        {
            while (recentMessages.Count > maxMessages)
            {
                recentMessages.RemoveAt(0);
            }
        }
    }
}
