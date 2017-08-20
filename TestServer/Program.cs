using Microsoft.Extensions.DependencyInjection;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestServer
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
                .AddTransient<TestServer>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            TestServer server = serviceProvider.GetService<TestServer>();
            server.RunService(args);

            System.Environment.Exit(0);
        }
    }
}