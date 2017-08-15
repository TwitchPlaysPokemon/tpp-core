using Microsoft.Extensions.DependencyInjection;
using TPPCommon.PubSub;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IPublisher, ZMQPublisher>()
                .AddTransient<TestServer>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run server.
            TestServer server = serviceProvider.GetService<TestServer>();
            server.Run();
        }
    }
}