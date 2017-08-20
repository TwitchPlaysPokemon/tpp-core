using System.Collections.Generic;
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
        private const string BaseConfigFilename = "config_tpp_base.yaml";

        /// <summary>
        /// Populate the configuration settings by reading the YAML config files.
        /// 
        /// The given configs will be read in a hierarchical order, meaning settings in
        /// the earlier config file can be overridden in later config files.
        /// </summary>
        /// <typeparam name="T">type of settings</typeparam>
        /// <param name="configFilenames">list of config filenames, in descending hierarchical order</param>
        /// <returns>config object</returns>
        public T ReadConfig<T>(params string[] configFilenames)
        {
            // Add the base config file to the front of the list of config filenames.
            List<string> filenames = new List<string>() { YamlConfigReader.BaseConfigFilename };
            filenames.AddRange(configFilenames);

            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Concatenate the contents of the config files.
            StringBuilder configContents = new StringBuilder();
            foreach (string filename in filenames)
            {
                string filepath = Path.Combine(assemblyDirectory, YamlConfigReader.BaseConfigDirectory, filename);
                configContents.AppendLine(File.ReadAllText(filepath));
            }

            Deserializer deserializer = new DeserializerBuilder().Build();

            T config = deserializer.Deserialize<T>(configContents.ToString());
            if (config == null)
            {
                string message = $"Failed to read configs for: {string.Join(", ", filenames)}";
                throw new InvalidConfigurationException(message);
            }

            return config;
        }
    }
}
