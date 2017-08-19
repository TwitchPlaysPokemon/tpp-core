using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Give time for PubSubBroker to warm up.
            Thread.Sleep(2000);

            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IPublisher, ZMQPublisher>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<ITPPLoggerFactory, TPPDebugLoggerFactory>()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<TestServer>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run server.
            TestServer server = serviceProvider.GetService<TestServer>();
            server.Run();
        }
    }
}