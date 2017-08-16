using System;
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

        public TestServer(IPublisher publisher)
        {
            this.Publisher = publisher;
        }

        public void Run()
        {
            Console.WriteLine("Running Music Server, Enter keys to publish events...");

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

                // Decide which event to publish.
                if (input == "A")
                {
                    Console.WriteLine($"Sending Song Paused Event...");
                    this.Publisher.Publish(new SongPausedEvent());
                }
                else
                {
                    Console.WriteLine($"Sending Song Info Event...");
                    SongInfoEvent @event = new SongInfoEvent(10, "Battle Theme", "Game Freak");
                    this.Publisher.Publish(@event);
                }
            }
        }
    }
}
