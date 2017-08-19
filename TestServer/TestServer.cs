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
        private ITPPLogger Logger;

        public TestServer(IPublisher publisher, ITPPLogger logger)
        {
            this.Publisher = publisher;
            this.Logger = logger;
        }

        public void Run()
        {
            this.Logger.SetLogPrefix("(TestServer) ");
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
