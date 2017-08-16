using System;
using System.Threading;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Messages;

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
            this.Subscriber.Subscribe<SongInfoMessage>(OnSongInfoChanged);
            this.Subscriber.Subscribe<SongPausedEvent>(OnSongPaused);

            // Run forever.
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        void OnSongInfoChanged(SongInfoMessage message)
        {
            this.TotalMessagesReceived += 1;
            Console.WriteLine($"Song Info:  Id = {message.Id}, Title = '{message.Title}', Artist = '{message.Artist}'");
            Console.WriteLine($"Total Messages Received: {this.TotalMessagesReceived}");
        }

        void OnSongPaused(SongPausedEvent message)
        {
            this.TotalMessagesReceived += 1;
            Console.WriteLine($"Song was paused!");
            Console.WriteLine($"Total Messages Received: {this.TotalMessagesReceived}");
        }
    }
}
