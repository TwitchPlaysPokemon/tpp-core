using System;
using System.Threading;
using TPPCommon.PubSub;

namespace TestClient
{
    /// <summary>
    /// Test service that subscribes to some pub-sub topics and prints out the published messages, while keeping
    /// track of the total number of messages it receives across all topics.
    /// </summary>
    class TestClient
    {
        private ISubscriber Subscriber;
        private int TotalMessagesReceived;

        public TestClient(ISubscriber subscriber)
        {
            this.Subscriber = subscriber;
            this.TotalMessagesReceived = 0;
        }

        public void Run()
        {
            Console.WriteLine("Running Subscriber client...");

            // Subscribe to the pub-sub topics, and assign message handler functions for each topic.
            this.Subscriber.Subscribe(Topic.Topic1, PrintTopic1Message);
            this.Subscriber.Subscribe(Topic.Topic2, PrintTopic2Message);

            // Run forever.
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        // Topic1 handler
        void PrintTopic1Message(PubSubMessage message)
        {
            this.TotalMessagesReceived += 1;
            Console.WriteLine($"Topic1 Received: {message.Message}");
            Console.WriteLine($"Total Messages Received: {this.TotalMessagesReceived}");
        }

        // Topic2 handler
        void PrintTopic2Message(PubSubMessage message)
        {
            this.TotalMessagesReceived += 1;
            Console.WriteLine($"Topic2 Received: {message.Message}");
            Console.WriteLine($"Total Messages Received: {this.TotalMessagesReceived}");
        }
    }
}
