using Inputting;
using Models;

namespace Core
{
	public class Message
	{
		public User User { get; }
		public string MessageText { get; }
		public InputSequence? InputSequence { get; }

		public Message(User user, string messageText, InputSequence? inputSequence)
		{
			User = user;
			MessageText = messageText;
			InputSequence = inputSequence;
		}
	}
}
