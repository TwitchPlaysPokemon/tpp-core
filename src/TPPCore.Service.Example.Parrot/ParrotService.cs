using log4net;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace TPPCore.Service.Example.Parrot
{
    public class ParrotService : IService
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceContext context;
        private Model model;
        private ParrotWebHandler webHandler;
        private bool running = true;

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

            var jsonMessage = JObject.FromObject(new { message = message });
            context.PubSubClient.Publish(ParrotTopics.Broadcast, jsonMessage);

            model.Repeat();

            if (model.RepeatCount != 0 && model.RepeatCount % 5 == 0)
            {
                logger.DebugFormat("Broadcasting recent messages");

                var recentMessage = JObject.FromObject(new { recent = model.RecentMessages });
                context.PubSubClient.Publish(ParrotTopics.Recent, recentMessage);
            }
        }
    }
}
