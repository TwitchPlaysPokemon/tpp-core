using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace TPPCommon.Configuration
{
    /// <summary>
    /// Class responsible for reading configs from YAML files.
    /// </summary>
    public class YamlConfigReader : IConfigReader
    {
        private const string BaseConfigDirectory = "";
        private const string BaseConfigName = "config_tpp";

        /// <summary>
        /// Populate the configuration settings by reading the YAML config files.
        /// 
        /// The given configs will be read in a hierarchical order, meaning settings in
        /// the earlier config file can be overridden in later config files.
        /// 
        /// This will throw an exception when unexpected or missing config values are encountered, in order
        /// to guarantee the code is working with its expected configuration values.
        /// </summary>
        /// <typeparam name="T">type of settings</typeparam>
        /// <param name="configNames">list of config names, in descending hierarchical order</param>
        /// <returns>config object</returns>
        public T ReadConfig<T>(params string[] configNames)
        {
            // Add the base config file to the front of the list of config filenames.
            List<string> allConfigNames = new List<string>() { YamlConfigReader.BaseConfigName };
            allConfigNames.AddRange(configNames);

            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Concatenate the contents of the config files.
            StringBuilder configContents = new StringBuilder();
            foreach (string configName in allConfigNames)
            {
                var configFilenames = new List<string>()
                {
                    configName + "_default.yaml",
                    configName + ".yaml",
                };

                foreach (string filename in configFilenames)
                {
                    string filepath = Path.Combine(assemblyDirectory, YamlConfigReader.BaseConfigDirectory, filename);
                    if (!File.Exists(filepath))
                    {
                        throw new InvalidConfigurationException($"Missing config file: {filepath}");
                    }

                    configContents.AppendLine(File.ReadAllText(filepath));
                }
            }

            Deserializer deserializer = new DeserializerBuilder().Build();

            T config = deserializer.Deserialize<T>(configContents.ToString());
            if (config == null)
            {
                string message = $"Failed to read configs for: {string.Join(", ", allConfigNames)}";
                throw new InvalidConfigurationException(message);
            }

            // Throws exception is any configs were missing or unexpected.
            ValidationContext context = new ValidationContext(config);
            Validator.ValidateObject(config, context, true);

            return config;
        }
    }
}
