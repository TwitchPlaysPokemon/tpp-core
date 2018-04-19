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

            Thread t = new Thread(ThreadProc)
            {
                Name = "Logging_Thread_1"
            };
            t.Start();

            context.PubSubClient.Subscribe(ChatTopics.Raw,
                (topic, message) =>
                {
                    LogManager.LogMessage(message);
                });
        }

        public void Run()
        {
            mre.Reset();
        }

        public void Shutdown()
        {
            mre.Set();
        }

        private static void ThreadProc()
        {
            mre.WaitOne();
        }
    }
}
