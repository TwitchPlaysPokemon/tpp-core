using TPPCore.Service.Chat;
using TPPCore.Service.Common;

namespace TPPCore.Service.ChatLogger
{
    public class ChatLoggerService : IService
    {
        private ServiceContext context;
        private bool running;

        public void Initialize(ServiceContext context)
        {
            this.context = context;
            running = true;
            LogManager.Configure(this.context);

            context.PubSubClient.Subscribe(ChatTopics.Raw,
                (topic, message) =>
                {
                    LogManager.LogMessage(message);
                });
        }

        public void Run()
        {
            while (running) ;
        }

        public void Shutdown()
        {
            running = false;
        }
    }
}
