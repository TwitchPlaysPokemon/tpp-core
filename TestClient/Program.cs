using Microsoft.Extensions.DependencyInjection;
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
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<ZMQSubscriber>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<TestClient>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            TestClient client = serviceProvider.GetService<TestClient>();
            client.Run();
        }
    }
}