using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using TPPCore.Service.Common.YamlUtils;
using TPPCore.Utils.Collections;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;

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

    public class ConfigReader : IReadOnlyDictionary<string[],string>
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly List<YamlDocument> YamlDocuments;

        private Dictionary<string[],string> configData;
        private Dictionary<string[],YamlNode> configNodes;
        private YamlMappingVisitor visitor;
        private Deserializer yamlDeserializer;

        public int Count { get { return configData.Count; } }
        public string this[string[] key] { get { return configData[key]; } }
        public IEnumerable<string[]> Keys { get { return configData.Keys; } }
        public IEnumerable<string> Values { get  { return configData.Values; } }

        public ConfigReader()
        {
            YamlDocuments = new List<YamlDocument>();
            var comparer = new StringEnumerableEqualityComparer<string[]>();
            configData = new Dictionary<string[], string>(comparer);
            configNodes = new Dictionary<string[], YamlNode>(comparer);
            visitor = new YamlMappingVisitor();
            visitor.ProcessKeyValuePair = addToConfig;
            yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
        }

        private void addToConfig(string[] key, YamlNode value)
        {
            configData.Add(key, value.ToString());
            configNodes.Add(key, value);
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

        public bool TryGetValue(string[] key, out string value)
        {
            return configData.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string[],string>> GetEnumerator()
        {
            return configData.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T GetCheckedValue<T>(string[] key)
        {
            YamlNode node;
            try
            {
                node = configNodes[key];
            }
            catch (KeyNotFoundException error)
            {
                throw new ConfigKeyNotFoundException(
                    $"The required key {string.Join(",", key)} was not found.",
                    error
                );
            }

            try
            {
                return yamlDeserializer.Deserialize<T>(
                    new EventStreamParserAdapter(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(node)));
            }
            catch (YamlException error)
            {
                throw new ConfigException(
                    $"The value at key {string.Join(",", key)} is not the correct type."
                    + $" Expected {typeof(T)} for value {node.ToString()}.", error);
            }
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
