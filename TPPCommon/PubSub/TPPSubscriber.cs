using TPPCommon.PubSub.Messages;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Class that manages subscribing to TPP PubSub messaging layer.
    /// </summary>
    public class TPPSubscriber
    {
        private ISubscriber Subscriber;

        public TPPSubscriber(ISubscriber subscriber)
        {
            this.Subscriber = subscriber;
        }

        /// <summary>
        /// Subscribe to the music service's current song info topic.
        /// </summary>
        /// <param name="handler">message handler</param>
        public void SubscribeToCurrentSongInfo(PubSubMessageHandler<SongInfoMessage> handler)
        {
            this.Subscriber.Subscribe(Topic.CurrentSongInfo, handler);
        }

        /// <summary>
        /// Subscribe to the music service's pause song event.
        /// </summary>
        /// <param name="handler">message handler</param>
        public void SubscribeToPauseSongEvent(PubSubMessageHandler<SongPausedEvent> handler)
        {
            this.Subscriber.Subscribe(Topic.EventSongPause, handler);
        }
    }
}
