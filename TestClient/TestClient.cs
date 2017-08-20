using System.Threading;
using TPPCommon;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TestClient
{
    /// <summary>
    /// Test service that subscribes to some pub-sub topics and prints out the published events, while keeping
    /// track of the total number of events it receives across all topics.
    /// </summary>
    class TestClient : TPPService
    {
        private TPPLoggerBase Logger;
        private TestClientConfig Config;
        private int TotalEventsReceived;

        protected override string[] ConfigNames => new string[] { "config_testclient" };
        protected override int StartupDelayMilliseconds => this.Config.StartupDelayMilliseconds;

        public TestClient(
            IPublisher publisher,
            ISubscriber subscriber,
            ITPPLoggerFactory loggerFactory,
            IConfigReader configReader) : base(publisher, subscriber, loggerFactory, configReader)
        { }

        protected override void Initialize()
        {
            this.Config = this.GetConfig<TestClientConfig>();
            this.Logger = this.LoggerFactory.Create(this.Config.ServiceName);
            this.TotalEventsReceived = 0;
        }

        protected override void Run()
        {
            this.Logger.LogInfo("Running Subscriber client...");

            // Subscribe to the pub-sub topics, and assign event handler functions for each topic.
            this.Subscriber.Subscribe<SongInfoEvent>(OnSongInfoChanged);
            this.Subscriber.Subscribe<SongPausedEvent>(OnSongPaused);

            // Block forever.
            new AutoResetEvent(false).WaitOne();
        }

        void OnSongInfoChanged(SongInfoEvent @event)
        {
            this.TotalEventsReceived += 1;
            this.Logger.LogInfo($"Song Info:  Id = {@event.Id}, Title = '{@event.Title}', Artist = '{@event.Artist}'");
            this.Logger.LogWarning($"Total Events Received: {this.TotalEventsReceived}");
        }

        void OnSongPaused(SongPausedEvent @event)
        {
            this.TotalEventsReceived += 1;
            this.Logger.LogInfo($"Song was paused!");
            this.Logger.LogWarning($"Total Events Received: {this.TotalEventsReceived}");
        }
    }
}
