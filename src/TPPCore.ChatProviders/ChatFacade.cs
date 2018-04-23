using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Service.Common;

namespace TPPCore.ChatProviders
{
    public class ChatFacade
    {
        private ServiceContext context;
        private Dictionary<string,IProvider> providers;

        public ChatFacade(ServiceContext context)
        {
            this.context = context;
            providers = new Dictionary<string, IProvider>();
        }

        public void RegisterProvider(IProvider provider)
        {
            providers[provider.ClientName] = provider;
        }

        public string GetUserId(string clientName) {
            var provider = providers[clientName];

            return provider.GetUserId();
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
                await ((IProviderAsync) provider).SendMessage(channel, message);
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
                await ((IProviderAsync) provider).SendPrivateMessage(user, message);
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
