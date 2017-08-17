using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Event for when the music service pauses the current song.
    /// </summary>
    [DataContract]
    [Topic("song_pause")]
    public class SongPausedEvent : PubSubEvent
    { }
}