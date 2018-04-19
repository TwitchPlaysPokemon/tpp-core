using System.Threading;
using TPPCore.Service.Chat;
using TPPCore.Service.Common;

namespace TPPCore.Service.ChatLogger
{
    public class ChatLoggerService : IService
    {
        private ServiceContext context;

        private static ManualResetEvent mre = new ManualResetEvent(false);

        public void Initialize(ServiceContext context)
        {
            this.context = context;
            LogManager.Configure(this.context);

            context.PubSubClient.Subscribe(ChatTopics.Raw,
                (topic, message) =>
                {
                    LogManager.LogMessage(message);
                });
        }

        public void Run()
        {
            mre.WaitOne();
        }

        public void Shutdown()
        {
            mre.Set();
        }
    }
}
