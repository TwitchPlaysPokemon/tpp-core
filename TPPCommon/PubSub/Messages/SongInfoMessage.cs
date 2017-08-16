using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Messages
{
    /// <summary>
    /// Pub-sub message object for the song info message.
    /// </summary>
    [DataContract]
    [Topic(Topic.CurrentSongInfo)]
    public class SongInfoMessage : PubSubMessage
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Artist { get; set; }

        public SongInfoMessage(int songId, string title, string artist)
        {
            this.Id = songId;
            this.Title = title;
            this.Artist = artist;
        }
    }
}
