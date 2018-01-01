using System.Threading.Tasks;

namespace TPPCommon.Chat.Client {
    /// <summary>
    /// A client for connecting to a chat server.
    /// Example servers include IRC servers, websocket endpoints.
    /// </summary>
    public interface IChatClient {
        Task ConnectAsync(ConnectionConfig config);
        void Disconnect();
        bool IsConnected();
        Task SendMessageAsync(ChatCommandMessage message);
        Task<ChatMessage> ReceiveMessageAsync();
    }
}
