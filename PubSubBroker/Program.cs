using Microsoft.Extensions.DependencyInjection;
using System;
using TPPCommon.PubSub;

namespace PubSubBroker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<IBroker, ZMQBroker>()
                .AddTransient<PubSubBroker>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Run client.
            PubSubBroker broker = serviceProvider.GetService<PubSubBroker>();
            broker.Run();
        }
    }
}