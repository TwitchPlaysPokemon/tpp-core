using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Messages
{
    /// <summary>
    /// Container for an Event pub-sub message.  Events are simple occurrences of a thing. They have no data attached to them.
    /// </summary>
    [DataContract]
    public abstract class PubSubEvent : PubSubMessage
    { }

    /// <summary>
    /// Event for when the music service pauses the current song.
    /// </summary>
    [DataContract]
    [Topic(Topic.EventSongPause)]
    public class SongPausedEvent : PubSubEvent
    { }
}
