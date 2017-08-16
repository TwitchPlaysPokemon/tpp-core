using Microsoft.Extensions.DependencyInjection;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Messages;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IPubSubMessageSerializer, JSONPubSubMessageSerializer>()
                .AddTransient<ZMQSubscriber>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<TPPSubscriber>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            TPPSubscriber tppSubscriber = serviceProvider.GetService<TPPSubscriber>();
            TestClient client = new TestClient(tppSubscriber);
            client.Run();
        }
    }
}