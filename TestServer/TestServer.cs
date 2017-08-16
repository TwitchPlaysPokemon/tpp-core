using System;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Messages;

namespace TestServer
{
    /// <summary>
    /// Test service that publishes messages to some pub-sub topics.
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
            Console.WriteLine("Running Music Server, Enter keys to publish messages...");

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

                // Decide which message to publish.
                if (input == "A")
                {
                    Console.WriteLine($"Sending Song Paused Event...");
                    this.Publisher.Publish(new SongPausedEvent());
                }
                else
                {
                    Console.WriteLine($"Sending Song Info Message...");
                    SongInfoMessage message = new SongInfoMessage(10, "Battle Theme", "Game Freak");
                    this.Publisher.Publish(message);
                }
            }
        }
    }
}
