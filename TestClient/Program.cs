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
                .AddTransient<ISubscriber, ZMQSubscriber>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            ISubscriber tppSubscriber = serviceProvider.GetService<ZMQSubscriber>();
            TestClient client = new TestClient(tppSubscriber);
            client.Run();
        }
    }
}