using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using TPPCore.Service.Common.YamlUtils;
using TPPCore.Utils.Collections;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace TPPCore.Service.Common
{
    public class ConfigException : Exception
    {
        public ConfigException() {}
        public ConfigException(string message) : base(message) {}
        public ConfigException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    public class ConfigKeyNotFoundException : ConfigException
    {
        public ConfigKeyNotFoundException() {}
        public ConfigKeyNotFoundException(string message) : base(message) {}
        public ConfigKeyNotFoundException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    public class ConfigReader : IReadOnlyDictionary<string[],object>
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly List<YamlDocument> YamlDocuments;

        private Dictionary<string[],object> configData;
        private YamlMappingVisitor visitor;

        public int Count { get { return configData.Count; } }
        public object this[string[] key] { get { return configData[key]; } }
        public IEnumerable<string[]> Keys { get { return configData.Keys; } }
        public IEnumerable<object> Values { get  { return configData.Values; } }

        public ConfigReader()
        {
            YamlDocuments = new List<YamlDocument>();
            var comparer = new StringEnumerableEqualityComparer<string[]>();
            configData = new Dictionary<string[], object>(comparer);
            visitor = new YamlMappingVisitor();
            visitor.ProcessKeyValuePair = addToConfig;
        }

        private void addToConfig(string[] key, object value)
        {
            configData.Add(key, value);
        }

        public void Load(string path)
        {
            logger.InfoFormat("Loading config file {0}", path);

            var yamlStream = new YamlStream();

            using (var stream = new StreamReader(path))
            {
                yamlStream.Load(stream);
            }

            processYamlStream(yamlStream);
        }

        public void LoadString(string content)
        {
            var yamlStream = new YamlStream();

            yamlStream.Load(new StringReader(content));

            processYamlStream(yamlStream);
        }

        void processYamlStream(YamlStream yamlStream)
        {
            foreach (var document in yamlStream)
            {
                YamlDocuments.Add(document);
            }

            yamlStream.Accept(visitor);
        }

        public bool ContainsKey(string[] key)
        {
            return configData.ContainsKey(key);
        }

        public bool TryGetValue(string[] key, out object value)
        {
            return configData.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string[],object>> GetEnumerator()
        {
            return configData.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T GetCheckedValue<T>(string[] key)
        {
            object value;
            try
            {
                value = configData[key];
            }
            catch (KeyNotFoundException error)
            {
                throw new ConfigKeyNotFoundException(
                    $"The required key {string.Join(",", key)} was not found.",
                    error
                );
            }

            if (!(value is T))
            {
                throw new ConfigException(
                    $"The key {string.Join(",", key)} is not the correct type.");
            }

            return (T) value;
        }

        public T GetCheckedValueOrDefault<T>(string[] key, T defaultValue)
        {
            try
            {
                return GetCheckedValue<T>(key);
            }
            catch (ConfigKeyNotFoundException)
            {
                return defaultValue;
            }
        }
    }
}
