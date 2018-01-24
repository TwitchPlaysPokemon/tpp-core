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
            Service.PubSubClient.Publish(chatEvent.Topic, chatEvent.ToJObject());
        }
    }
}
