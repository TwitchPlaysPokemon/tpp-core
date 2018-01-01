using System;
using TPPCommon;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestServer
{
    /// <summary>
    /// Test service that publishes events to some pub-sub topics.
    /// </summary>
    class TestServer : TPPService
    {
        private TestServerConfig Config;
        private TPPLoggerBase Logger;

        protected override string[] ConfigNames => new string[] { "config_testserver" };
        protected override int StartupDelayMilliseconds => this.Config.StartupDelayMilliseconds;

        public TestServer(
            IPublisher publisher,
            ISubscriber subscriber,
            ITPPLoggerFactory loggerFactory,
            IConfigReader configReader) : base(publisher, subscriber, loggerFactory, configReader)
        { }

        protected override void Initialize()
        {
            this.Config = this.GetConfig<TestServerConfig>();
            this.Logger = this.LoggerFactory.Create(this.Config.ServiceName);
        }

        protected override void Run()
        {
            this.Logger.LogInfo("Running Music Server, Enter keys to publish events...");

            while (true)
            {
                // Read single keystroke.
                ConsoleKeyInfo key = Console.ReadKey();
                string input = key.Key.ToString();
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    // Quit running the server.
                    break;
                }

                this.Logger.LogDebug($"The keystroke was {input}");

                // Decide which event to publish.
                if (input == this.Config.SongPauseKey)
                {
                    this.Logger.LogInfo($"Sending Song Paused Event...");
                    this.Publisher.Publish(new SongPausedEvent());
                }
                else
                {
                    this.Logger.LogInfo($"Sending Song Info Event...");
                    SongInfoEvent @event = new SongInfoEvent(10, "Battle Theme", "Game Freak");
                    this.Publisher.Publish(@event);
                }
            }
        }
    }
}
