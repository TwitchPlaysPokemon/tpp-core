using log4net;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using TPPCore.ChatProviders;
using TPPCore.Database;

namespace TPPCore.Service.Example.Parrot
{
    /// <summary>
    /// Service that repeatedly broadcasts a string.
    /// </summary>
    public class ParrotService : IServiceAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceContext context;
        private Model model;
        private ParrotWebHandler webHandler;
        private IDataProvider provider;
        private IParrotRepository repository;
        private DatabaseHandler handler;
        private bool running = true;
        private int broadcastInterval;
        private int recentIntervalCount;

        public ParrotService()
        {
            model = new Model();
        }

        public void Initialize(ServiceContext context)
        {
            this.context = context;

            string Database = context.ConfigReader.GetCheckedValue<string>("database", "database");
            string Host = context.ConfigReader.GetCheckedValue<string>("database", "host");
            string AppName = context.ConfigReader.GetCheckedValue<string>("database", "appname");
            string Username = context.ConfigReader.GetCheckedValue<string>("database", "username");
            string Password = context.ConfigReader.GetCheckedValue<string>("database", "password");
            int Port = context.ConfigReader.GetCheckedValue<int>("database", "port");
            provider = new PostgresqlDataProvider(Database, Host, AppName, Username, Password, Port);
            repository = new PostgresqlParrotRepository(provider);
            handler = new DatabaseHandler(repository);
            webHandler = new ParrotWebHandler(model, handler);

            repository.Configure(context);

            context.RestfulServer.UseRoute((RouteBuilder routeBuilder) =>
            {
                routeBuilder
                    .MapGet("message/recent", webHandler.GetRecent)
                    .MapGet("message/current", webHandler.GetCurrent)
                    .MapPost("message/new", webHandler.PostMessage)
                    .MapGet("message/database/getrecord/{id}", webHandler.GetRecord)
                    .MapGet("message/database/getmaxkey", webHandler.GetMaxId)
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

                await BroadcastMessage();
            }

            logger.Info("Parrot shutting down");
            await Task.Delay(broadcastInterval);
            logger.Info("Goodbye!");
        }

        private async Task BroadcastMessage()
        {
            var message = model.CurrentMessage;
            logger.DebugFormat("Broadcasting message {0}", message);

            var jsonMessage = JsonConvert.SerializeObject(message);
            context.PubSubClient.Publish(ParrotTopics.Broadcast, jsonMessage);
            await webHandler.SaveToDatabase(jsonMessage);

            model.Repeat();

            if (model.RepeatCount != 0 && model.RepeatCount % recentIntervalCount == 0)
            {
                logger.DebugFormat("Broadcasting recent messages");

                var recentMessage = JsonConvert.SerializeObject(model.RecentMessages);
                context.PubSubClient.Publish(ParrotTopics.Recent, recentMessage);
            }
        }
    }
}
