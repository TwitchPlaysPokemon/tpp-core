using System;
using TPPCommon.PubSub;

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
            Console.WriteLine("Running Server, Enter keys to publish messages...");

            while (true)
            {
                // Read single keystroke.
                ConsoleKeyInfo key = Console.ReadKey();
                string message = key.Key.ToString();
                if (message.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    // Quit running the server.
                    break;
                }

                Console.WriteLine($"Publishing {message}...");

                // Decide which topic to publish the message to.
                if (message == "A")
                {
                    this.Publisher.PublishMessageString(Topic.Topic1, message);
                }
                else
                {
                    this.Publisher.PublishMessageString(Topic.Topic2, message);
                }
            }
        }
    }
}
