using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Service.Common;

namespace TPPCore.ChatProviders
{
    public class ChatFacade
    {
        private readonly ServiceContext context;
        private readonly Dictionary<string, IProvider> providers;

        public ChatFacade(ServiceContext context)
        {
            this.context = context;
            providers = new Dictionary<string, IProvider>();
        }

        public void RegisterProvider(IProvider provider)
        {
            providers[provider.ClientName] = provider;
        }

        public async Task<string> GetUserId(string clientName) {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                return (provider as IProviderThreaded).GetUserId();
            }
            else
            {
                return await ((IProviderAsync)provider).GetUserId();
            }
        }

        public string GetUsername(string clientName) {
            var provider = providers[clientName];

            return provider.GetUsername();
        }

        public async Task SendMessage(string clientName, string channel, string message)
        {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).SendMessage(channel, message);
            }
            else
            {
                await ((IProviderAsync)provider).SendMessage(channel, message);
            }
        }

        public async Task SendPrivateMessage(string clientName, string user, string message)
        {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).SendPrivateMessage(user, message);
            }
            else
            {
                await ((IProviderAsync)provider).SendPrivateMessage(user, message);
            }
        }

        public async Task TimeoutUser(string clientName, ChatUser user, string reason, int duration, string channel)
        {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).TimeoutUser(user, reason, duration, channel);
            }
            else
            {
                await ((IProviderAsync)provider).TimeoutUser(user, reason, duration, channel);
            }
        }

        public async Task BanUser(string clientName, ChatUser user, string reason, string channel)
        {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).BanUser(user, reason, channel);
            }
            else
            {
                await ((IProviderAsync)provider).BanUser(user, reason, channel);
            }
        }

        public async Task<IList<ChatUser>> GetRoomList(string clientName, string channel)
        {
            var provider = providers[clientName];

            if (provider is IProviderThreaded)
            {
                return (provider as IProviderThreaded).GetRoomList(channel);
            }
            else
            {
                return await ((IProviderAsync) provider).GetRoomList(channel);
            }
        }
    }
}
