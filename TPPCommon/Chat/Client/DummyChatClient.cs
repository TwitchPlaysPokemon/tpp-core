using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TPPCommon.Chat.Client {
    public class DummyChatClient : IChatClient
    {
        private int dummyReceiveCounter = 0;
        private bool _isConnected = false;

        public DummyChatClient()
        {
        }

        public async Task ConnectAsync(ConnectionConfig config)
        {
            if (dummyReceiveCounter % 2 == 1) {
                throw new SocketException(1);
            }

            _isConnected = true;
            dummyReceiveCounter++;
            await Task.Delay(1000);
        }

        public void Disconnect()
        {
            _isConnected = false;
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        public async Task<ChatMessage> ReceiveMessageAsync()
        {
            if (!_isConnected) {
                throw new InvalidOperationException("Not connected");
            }

            await Task.Delay(10);

            var message = new ChatMessage();
            dummyReceiveCounter++;

            return message;
        }

        public async Task SendMessageAsync(ChatCommandMessage message)
        {
            if (!_isConnected) {
                throw new InvalidOperationException("Not connected");
            }

            await Task.Delay(50);
        }
    }
}
