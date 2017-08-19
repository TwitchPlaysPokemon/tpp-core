using System;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestServer
{
    /// <summary>
    /// Test service that publishes events to some pub-sub topics.
    /// </summary>
    class TestServer
    {
        private IPublisher Publisher;
        private ITPPLoggerFactory LoggerFactory;
        private TPPLoggerBase Logger;
        private TPPLoggerBase Logger2;

        public TestServer(IPublisher publisher, ITPPLoggerFactory loggerFactory)
        {
            this.Publisher = publisher;
            this.LoggerFactory = loggerFactory;
            this.Logger = this.LoggerFactory.Create("test_server");
            this.Logger2 = this.LoggerFactory.Create("test_server_2");
        }

        public void Run()
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
                if (input == "A")
                {
                    this.Logger2.LogInfo($"Sending Song Paused Event...");
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
