using Newtonsoft.Json;
using System.Diagnostics;
using TPPCore.Service.Common;

namespace TPPCore.ChatProviders
{
    public class ProviderContext
    {
        public readonly ServiceContext Service;
        public readonly ChatFacade Chat;

        public ProviderContext(ServiceContext serviceContext, ChatFacade chatFacade)
        {
            Service = serviceContext;
            Chat = chatFacade;
        }

        public void PublishChatEvent(IPubSubEvent chatEvent)
        {
            Debug.Assert(chatEvent.Topic != null);
            Service.PubSubClient.Publish(chatEvent.Topic, JsonConvert.SerializeObject(chatEvent));
        }
    }
}
