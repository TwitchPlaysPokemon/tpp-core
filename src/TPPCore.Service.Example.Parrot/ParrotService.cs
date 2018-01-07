using log4net;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace TPPCore.Service.Example.Parrot
{
    public class ParrotService : IServiceAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceContext context;
        private Model model;
        private ParrotWebHandler webHandler;
        private bool running = true;
        private int broadcastInterval;
        private int recentIntervalCount;

        public ParrotService()
        {
            model = new Model();
            webHandler = new ParrotWebHandler(model);
        }

        public void Initialize(ServiceContext context)
        {
            this.context = context;

            context.RestfulServer.UseRoute((RouteBuilder routeBuilder) =>
            {
                routeBuilder
                    .MapGet("message/recent", webHandler.GetRecent)
                    .MapGet("message/current", webHandler.GetCurrent)
                    .MapPost("message/new", webHandler.PostMessage)
                    ;
            });

            broadcastInterval = context.ConfigReader.GetCheckedValueOrDefault<int>(
                new[] {"parrot", "broadcastInterval"}, 1000);
            recentIntervalCount =  context.ConfigReader.GetCheckedValueOrDefault<int>(
                new[] {"parrot", "recentIntervalCount"}, 5);

            context.PubSubClient.Subscribe(ParrotTopics.Broadcast,
                (topic, message) =>
                {
                    logger.DebugFormat("Pub/sub echo {0}: {1}", topic, message);
                });
        }

        public void Run()
        {
            RunAsync().Wait();
        }

        public void Shutdown()
        {
            running = false;
        }

        public async Task RunAsync()
        {
            while (running)
            {
                await Task.Delay(broadcastInterval);

                broadcastMessage();
            }

            logger.Info("Parrot shutting down");
            await Task.Delay(broadcastInterval);
            logger.Info("Goodbye!");
        }

        private void broadcastMessage()
        {
            var message = model.CurrentMessage;
            logger.DebugFormat("Broadcasting message {0}", message);

            var jsonMessage = JObject.FromObject(new { message = message });
            context.PubSubClient.Publish(ParrotTopics.Broadcast, jsonMessage);

            model.Repeat();

            if (model.RepeatCount != 0 && model.RepeatCount % recentIntervalCount == 0)
            {
                logger.DebugFormat("Broadcasting recent messages");

                var recentMessage = JObject.FromObject(new { recent = model.RecentMessages });
                context.PubSubClient.Publish(ParrotTopics.Recent, recentMessage);
            }
        }
    }
}
