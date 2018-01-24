using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Chat
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
            providers[provider.Name] = provider;
        }

        public string GetUserId(string providerName) {
            var provider = providers[providerName];

            return provider.GetUserId();
        }

        public async Task SendMessage(string providerName, string channel, string message)
        {
            var provider = providers[providerName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).SendMessage(channel, message);
            }
            else
            {
                await ((IProviderAsync) provider).SendMessage(channel, message);
            }
        }

        public async Task SendPrivateMessage(string providerName, string user, string message)
        {
            var provider = providers[providerName];

            if (provider is IProviderThreaded)
            {
                (provider as IProviderThreaded).SendPrivateMessage(user, message);
            }
            else
            {
                await ((IProviderAsync) provider).SendPrivateMessage(user, message);
            }
        }

        public async Task<IList<ChatUser>> GetRoomList(string providerName, string channel)
        {
            var provider = providers[providerName];

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
