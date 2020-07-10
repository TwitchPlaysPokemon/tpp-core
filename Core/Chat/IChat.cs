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

    public interface IChat
    {
        event EventHandler<MessageEventArgs> IncomingMessage;

        /// <summary>
        /// Establishes the connection and keeps it alive until the returned disposable is disposed.
        /// This method may only ever be called once per instance of this class.
        /// </summary>
        /// <returns>A disposable that properly disconnects and cleans up the connection when disposed.</returns>
        IDisposable EstablishConnection();

        Task SendMessage(string message);
        Task SendWhisper(User target, string message);
    }
}
