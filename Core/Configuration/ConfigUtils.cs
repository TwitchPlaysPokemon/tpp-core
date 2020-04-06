using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Core.Configuration
{
    /// <summary>
    /// Various configuration related utility functions.
    /// </summary>
    public static class ConfigUtils
    {
        public static void LogUnrecognizedConfigs(ILogger logger, ConfigBase config)
        {
            LogUnrecognizedConfigs(logger, config, new List<string>());
        }

        private static void LogUnrecognizedConfigs(ILogger logger, ConfigBase config, IList<string> parentConfigKeys)
        {
            foreach (string configKey in config.UnrecognizedConfigs.Keys)
            {
                string fullyQualifiedConfigKey = string.Join(".", parentConfigKeys.Concat(new[] {configKey}));
                logger.LogWarning($"unrecognized config key '{fullyQualifiedConfigKey}'");
            }
            // recursively check all nested configs
            foreach (var property in config.GetType().GetProperties())
            {
                if (property.PropertyType.IsSubclassOf(typeof(ConfigBase)))
                {
                    var value = (ConfigBase) property.GetValue(config)!;
                    LogUnrecognizedConfigs(logger, value, parentConfigKeys.Concat(new[] {property.Name}).ToList());
                }
            }
        }
    }
}
