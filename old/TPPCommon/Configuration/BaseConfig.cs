using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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
        /// <param name="configOverrides">overrides for specific config values</param>
        /// <param name="configFileOverride">additional config file, whose values will take priority over other files</param>
        /// <param name="configNames">list of config names, in descending hierarchical order</param>
        /// <returns>config settings</returns>
        public static T GetConfig<T>(IConfigReader configReader, IDictionary<string, string> configOverrides, string configFileOverride, params string[] configNames) where T : BaseConfig
        {
            return configReader.ReadConfig<T>(configOverrides, configFileOverride, configNames);
        }

        /// <summary>
        /// Get the property on the config object for the specified config name.
        /// </summary>
        /// <typeparam name="T">config object type</typeparam>
        /// <param name="configName">config name</param>
        /// <returns>config object's property</returns>
        internal static PropertyInfo GetConfigProperty(Type configType, string configName)
        {
            return configType.GetRuntimeProperties()
                    .Where(prop => prop.IsDefined(typeof(YamlMemberAttribute), true))
                    .FirstOrDefault(prop =>
                        prop.CustomAttributes.Any(attr =>
                            attr.NamedArguments.Any(arg => arg.MemberName.Equals("Alias") && arg.TypedValue.Value.Equals(configName))));
        }
    }
}
