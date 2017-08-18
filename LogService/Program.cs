using Microsoft.Extensions.DependencyInjection;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace LogService
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection, to hide implementations.
            var serviceCollection = new ServiceCollection()
                .AddTransient<ILogger, Log4NetLogger>()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<ZMQSubscriber>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<LogService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            LogService logService = serviceProvider.GetService<LogService>();
            logService.Run();
        }
    }
}