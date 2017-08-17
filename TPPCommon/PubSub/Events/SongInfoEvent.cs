using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Pub-sub event class for the song info event.
    /// </summary>
    [DataContract]
    [Topic("song_info")]
    public class SongInfoEvent : PubSubEvent
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Artist { get; set; }

        public SongInfoEvent(int songId, string title, string artist)
        {
            this.Id = songId;
            this.Title = title;
            this.Artist = artist;
        }
    }
}
