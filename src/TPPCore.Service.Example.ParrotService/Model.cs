using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TPPCore.Service.Example.ParrotService
{
    class Model
    {
        public ReadOnlyCollection<string> RecentMessages
        {
            get { return recentMessages.AsReadOnly(); }
        }
        public string CurrentMessage
        {
            get { return currentMessage; }
            set
            {
                currentMessage = value;
                repeatCount = 0;
                recentMessages.Add(value);
            }
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

        public void Increment()
        {
            repeatCount += 1;
            recentMessages.Add(currentMessage);
        }
    }
}
