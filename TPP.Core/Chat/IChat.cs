using System;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Core.Chat
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

    public interface IChatModeChanger
    {
        public Task EnableEmoteOnly();
        public Task DisableEmoteOnly();
    }

    public interface IChat : IMessageSender, IChatModeChanger, IDisposable
    {
        string Name { get; }

        event EventHandler<MessageEventArgs> IncomingMessage;

        /// Establishes the connection.
        /// All subsequent repeated invocations on this instance will fail.
        /// The connection gets closed by disposing this instance.
        void Connect();
    }
}
