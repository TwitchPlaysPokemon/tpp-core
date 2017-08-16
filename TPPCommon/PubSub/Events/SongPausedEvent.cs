using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Event for when the music service pauses the current song.
    /// </summary>
    [DataContract]
    [Topic(Topic.EventSongPause)]
    public class SongPausedEvent : PubSubEvent
    { }
}