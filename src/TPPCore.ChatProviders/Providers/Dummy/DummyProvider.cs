using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.ChatProviders.Providers.Dummy
{
    public class DummyProvider : IProviderAsync
    {
        public string ClientName { get; private set; }
        public string ProviderName { get; } = "dummy";

        private bool running = true;
        private ProviderContext context;
        private int chatNoiseCounter = 2;

        public void Configure(string clientName, ProviderContext providerContext)
        {
            this.ClientName = clientName;
            this.context = providerContext;
        }

        public async Task Run()
        {
            while (running)
            {
                foreach (var i in Enumerable.Range(0, chatNoiseCounter))
                {
                    var chatMessage = NewFakeReceivedMessage();
                    context.PublishChatEvent(chatMessage);
                }

                chatNoiseCounter *= 2;
                await Task.Delay(1000);
            }
        }

        public void Shutdown()
        {
            running = false;
        }
#pragma warning disable 1998
        public async Task<string> GetUserId()
        {
            return "dummy";
        }
#pragma warning restore 1998
        public string GetUsername()
        {
            return "dummy";
        }

        public async Task SendMessage(string channel, string message)
        {
            await Task.Delay(100);
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message,
                Channel = channel,
                IsSelf = true
            };

            context.PublishChatEvent(chatMessage);
        }

        public async Task SendPrivateMessage(string user, string message)
        {
            await Task.Delay(100);
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message,
                Channel = user,
                IsSelf = true
            };
            context.PublishChatEvent(chatMessage);
        }

        public async Task TimeoutUser(ChatUser user, string reason, int duration, string channel)
        {
            await Task.Delay(100);
            var chatMessage = new ChatMessage()
            {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = $"/timeout {user.Username} {duration} {reason}",
                Channel = channel,
                IsSelf = true,
            };
            context.PublishChatEvent(chatMessage);
        }

        public async Task BanUser(ChatUser user, string reason, string channel)
        {
            await Task.Delay(100);
            var chatMessage = new ChatMessage()
            {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = $"/ban {user.Username} {reason}",
                Channel = channel,
                IsSelf = true
            };
            context.PublishChatEvent(chatMessage);
        }

        public async Task<IList<ChatUser>> GetRoomList(string channel)
        {
            await Task.Delay(100);
            var user1 = new ChatUser()
            {
                UserId = "dummy",
                Username = "dummy",
                Nickname = "Dummy",
                AccessLevel = AccessLevel.Viewer,
            };
            var user2 = new ChatUser()
            {
                UserId = "someone",
                Username = "someone",
                Nickname = "Someone",
                AccessLevel = AccessLevel.Moderator,
            };
            return new List<ChatUser>() { user1, user2 };
        }

        private ChatMessage NewFakeReceivedMessage() {
            var random = new Random();
            var userId = random.Next(0, 1000000).ToString();

            var user = new ChatUser() {
                UserId = userId,
                Username = $"someone{userId}",
                Nickname = $"Someone{userId}",
            };
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                Sender = user,
                TextContent = "hello " + random.Next(0, 10000)
            };

            return chatMessage;
        }
    }
}
