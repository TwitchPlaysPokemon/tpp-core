using System;
using System.Collections.Generic;
using System.Linq;

namespace TPP.Core.Configuration;

/// <summary>
/// Various configuration related utility functions.
/// </summary>
public static class ConfigUtils
{
    public static void WriteUnrecognizedConfigsToStderr(ConfigBase config)
    {
        WriteUnrecognizedConfigsToStderr(config, new List<string>());
    }

    private static void WriteUnrecognizedConfigsToStderr(ConfigBase config, IList<string> parentConfigKeys)
    {
        foreach (string configKey in config.UnrecognizedConfigs.Keys)
        {
            if (config is IRootConfig && configKey == IRootConfig.SchemaFieldName) continue;
            string fullyQualifiedConfigKey = string.Join(".", parentConfigKeys.Concat(new[] { configKey }));
            Console.Error.WriteLine($"unrecognized config key '{fullyQualifiedConfigKey}'");
        }
        // recursively check all nested configs
        foreach (var property in config.GetType().GetProperties())
        {
            if (property.PropertyType.IsSubclassOf(typeof(ConfigBase)))
            {
                var value = (ConfigBase?)property.GetValue(config);
                if (value != null)
                {
                    WriteUnrecognizedConfigsToStderr(value,
                        parentConfigKeys.Concat(new[] { property.Name }).ToList());
                }
            }
        }
    }
}
