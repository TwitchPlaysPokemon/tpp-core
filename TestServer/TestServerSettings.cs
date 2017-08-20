using TPPCommon.Configuration;
using YamlDotNet.Serialization;

namespace TestServer
{
    /// <summary>
    /// Class responsible holding configuration settings for the test server.
    /// </summary>
    internal class TestServerSettings : BaseConfig
    {
        [YamlMember(Alias = "song_pause_key")]
        public string SongPauseKey { get; set; }
    }
}
