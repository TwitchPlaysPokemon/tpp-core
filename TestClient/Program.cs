using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Give time for PubSubBroker to warm up.
            Thread.Sleep(2000);

            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<ZMQSubscriber>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<IPublisher, ZMQPublisher>()
                .AddTransient<TPPLogger>()
                .AddTransient<TestClient>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            TestClient client = serviceProvider.GetService<TestClient>();
            client.Run();
        }
    }
}