using System.ComponentModel.DataAnnotations;
using TPPCommon.Configuration;
using YamlDotNet.Serialization;

namespace TestServer
{
    /// <summary>
    /// Class responsible holding configuration settings for the test server.
    /// </summary>
    internal class TestServerConfig : BaseConfig
    {
        [YamlMember(Alias = "song_pause_key")]
        [Required]
        public string SongPauseKey { get; set; }
    }
}
