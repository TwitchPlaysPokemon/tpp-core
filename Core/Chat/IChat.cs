using System;
using System.Threading.Tasks;
using Persistence.Models;

namespace Core.Chat
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; }
        public MessageEventArgs(Message message) => Message = message;
    }

    public interface IMessageSender
    {
        Task SendMessage(string message);
        Task SendWhisper(User target, string message);
    }

    public interface IChat : IMessageSender, IDisposable
    {
        event EventHandler<MessageEventArgs> IncomingMessage;
        event EventHandler<string> IncomingUnhandledIrcLine;

        /// Establishes the connection.
        /// All subsequent repeated invocations on this instance will fail.
        /// The connection gets closed by disposing this instance.
        void Connect();
    }
}
