using Microsoft.Extensions.DependencyInjection;
using TPPCommon.Configuration;
using TPPCommon.Logging;
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
                .AddTransient<IPublisher, ZMQPublisher>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<ITPPLoggerFactory, TPPDebugLoggerFactory>()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<IConfigReader, YamlConfigReader>()
                .AddTransient<ILogger, Log4NetLogger>()
                .AddTransient<LogService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            LogService logService = serviceProvider.GetService<LogService>();
            logService.RunService();
        }
    }
}