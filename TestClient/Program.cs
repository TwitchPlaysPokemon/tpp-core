using Microsoft.Extensions.DependencyInjection;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IPublisher, ZMQPublisher>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<ITPPLoggerFactory, TPPDebugLoggerFactory>()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<IConfigReader, YamlConfigReader>()
                .AddTransient<TestClient>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            TestClient client = serviceProvider.GetService<TestClient>();
            client.RunService();

            System.Environment.Exit(0);
        }
    }
}