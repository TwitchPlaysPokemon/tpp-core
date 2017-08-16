using System;
using System.Threading;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestClient
{
    /// <summary>
    /// Test service that subscribes to some pub-sub topics and prints out the published events, while keeping
    /// track of the total number of events it receives across all topics.
    /// </summary>
    class TestClient
    {
        private ISubscriber Subscriber;
        private int TotalEventsReceived;

        public TestClient(ISubscriber subscriber)
        {
            this.Subscriber = subscriber;
            this.TotalEventsReceived = 0;
        }

        public void Run()
        {
            Console.WriteLine("Running Subscriber client...");

            // Subscribe to the pub-sub topics, and assign event handler functions for each topic.
            this.Subscriber.Subscribe<SongInfoEvent>(OnSongInfoChanged);
            this.Subscriber.Subscribe<SongPausedEvent>(OnSongPaused);

            // Run forever.
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        void OnSongInfoChanged(SongInfoEvent @event)
        {
            this.TotalEventsReceived += 1;
            Console.WriteLine($"Song Info:  Id = {@event.Id}, Title = '{@event.Title}', Artist = '{@event.Artist}'");
            Console.WriteLine($"Total Events Received: {this.TotalEventsReceived}");
        }

        void OnSongPaused(SongPausedEvent @event)
        {
            this.TotalEventsReceived += 1;
            Console.WriteLine($"Song was paused!");
            Console.WriteLine($"Total Events Received: {this.TotalEventsReceived}");
        }
    }
}
