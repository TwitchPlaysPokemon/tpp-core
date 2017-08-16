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
                .AddTransient<ISubscriber, ZMQSubscriber>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            ISubscriber tppSubscriber = serviceProvider.GetService<ZMQSubscriber>();
            TestClient client = new TestClient(tppSubscriber);
            client.Run();
        }
    }
}