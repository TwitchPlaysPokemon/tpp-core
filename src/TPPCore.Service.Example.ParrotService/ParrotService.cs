using log4net;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Example.ParrotService
{
    class ParrotService : IService
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceContext context;
        private Model model;
        private bool running = true;

        public ParrotService()
        {
            model = new Model();
        }

        public void Initialize(ServiceContext context)
        {
            this.context = context;

            // TODO: add REST functionality:
            // * GET /recent
            // * GET /current
            // * POST /message
        }

        public void Run()
        {
            var task = runAsync();
            task.Wait();
        }

        public void Shutdown()
        {
            running = false;
        }

        private async Task runAsync()
        {
            while (running)
            {
                await Task.Delay(1000);

                broadcastMessage();
            }

            logger.Info("Parrot shutting down");
            await Task.Delay(2000);
            logger.Info("Goodbye!");
        }

        private void broadcastMessage()
        {
            var message = model.CurrentMessage;
            logger.DebugFormat("Broadcasting message {0}", message);

            var jsonMessage = new JObject();
            jsonMessage.Add("message", message);
            context.PubSubClient.Publish(ParrotTopics.Broadcast, jsonMessage.ToString());

            model.Repeat();

            if (model.RepeatCount != 0 && model.RepeatCount % 5 == 0)
            {
                logger.DebugFormat("Broadcasting recent messages");

                var recentMessage = new JObject();
                recentMessage.Add("recent", new JArray(model.RecentMessages));
                context.PubSubClient.Publish(ParrotTopics.Recent, recentMessage.ToString());
            }
        }
    }
}
