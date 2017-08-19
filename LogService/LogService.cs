﻿using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace LogService
{
    /// <summary>
    /// Central service responsible for handling all TPP logging events, and writing them to the appropriate places.
    /// </summary>
    internal class LogService
    {
        private ISubscriber Subscriber;
        private ILogger Logger;

        public LogService(ISubscriber subscriber, ILogger logger)
        {
            this.Subscriber = subscriber;
            this.Logger = logger;
        }

        internal void Run()
        {
            // Setup dependency injection, to hide the pub-sub implementation.
            var serviceCollection = new ServiceCollection()
                .AddTransient<ILogger, Log4NetLogger>()
                .AddTransient<ISubscriber, ZMQSubscriber>()
                .AddTransient<LogService>()
                .AddTransient<IPubSubEventSerializer, JSONPubSubEventSerializer>()
                .AddTransient<ZMQPublisher>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Subscribe to all log events.
            this.Subscriber.Subscribe<LogDebugEvent>(log => this.Logger.LogDebug(log.Message));
            this.Subscriber.Subscribe<LogInfoEvent>(log => this.Logger.LogInfo(log.Message));
            this.Subscriber.Subscribe<LogWarningEvent>(log => this.Logger.LogWarning(log.Message));
            this.Subscriber.Subscribe<LogErrorEvent>(log => this.Logger.LogError(log.Message, log.Exception));
            this.Subscriber.Subscribe<LogCriticalEvent>(log => this.Logger.LogCritical(log.Message, log.Exception));

            // Block forever.
            new AutoResetEvent(false).WaitOne();
        }
    }
}
