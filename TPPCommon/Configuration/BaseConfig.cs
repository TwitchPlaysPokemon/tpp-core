using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace TPPCommon.Configuration
{
    /// <summary>
    /// Class responsible for holding configuration settings for a TPP service.
    /// </summary>
    public abstract class BaseConfig
    {
        [YamlMember(Alias = "service_name")]
        [Required]
        public string ServiceName { get; set; }

        [YamlMember(Alias = "startup_delay_ms")]
        [Required]
        public int StartupDelayMilliseconds { get; set; }

        /// <summary>
        /// Gets the populated configuration settings object.
        /// </summary>
        /// <param name="configReader">config reader</param>
        /// <param name="configFilenames">list of config filenames, in descending hierarchical order</param>
        /// <returns>config settings</returns>
        public static T GetConfig<T>(IConfigReader configReader, params string[] configFilenames) where T : BaseConfig
        {
            return configReader.ReadConfig<T>(configFilenames);
        }
    }
}
