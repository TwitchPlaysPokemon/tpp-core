using System.Diagnostics;
using TPPCore.Service.Common;

namespace TPPCore.Service.Chat
{
    public class ProviderContext
    {
        public readonly ServiceContext Service;
        public readonly ChatFacade Chat;

        public ProviderContext(ServiceContext serviceContext, ChatFacade chatFacade)
        {
            this.Service = serviceContext;
            this.Chat = chatFacade;
        }

        public void PublishChatEvent(IPubSubEvent chatEvent)
        {
            Debug.Assert(chatEvent.Topic != null);
            Service.PubSubClient.Publish(chatEvent.Topic, chatEvent.ToJObject());
        }
    }
}
