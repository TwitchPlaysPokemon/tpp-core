using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

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

    /// <summary>
    /// Reads configuration from JSON files and stores them in a list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class stores jsons as string arrays, but
    /// typical usage is intended with <see cref="GetCheckedValue"> that
    /// automatically parses and converts values.
    /// </para>
    /// </remarks>
    public class ConfigReader : IList<string>
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// JSON model that is read from the config.
        /// </summary>
        public int Count => Jsons.Count;
        public string this[int index] { get => Jsons[index]; set => Jsons[index] = value; }
        public bool IsReadOnly => false; 

        private List<string> Jsons;

        public ConfigReader()
        {
            Jsons = new List<string>();
        }

        /// <summary>
        /// Read configuration from a JSON file.
        /// </summary>
        public void Load(string path)
        {
            logger.InfoFormat("Loading config file {0}", path);

            try
            {
                string content;
                using (StreamReader reader = new StreamReader(path))
                {
                    content = reader.ReadToEnd();
                }

                JsonConvert.DeserializeObject(content);
                Jsons.Add(content);
            }
            catch (Exception exception)
            {
                throw new ConfigException("Failed to deserialize json.", exception);
            }

        }

        /// <summary>
        /// Read configuration from a stream containg JSON markup.
        /// </summary>
        public void Load(Stream stream)
        {
            try
            {
                string content;
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                JsonConvert.DeserializeObject(content);
                Jsons.Add(content);
            }
            catch (Exception exception)
            {
                throw new ConfigException("Failed to deserialize json.", exception);
            }
        }

        /// <summary>
        /// Read configuration from a JSON string.
        /// </summary>
        public void LoadString(string content)
        {
            try
            {
                JsonConvert.DeserializeObject(content);
                Jsons.Add(content);
            }
            catch (Exception exception)
            {
                throw new ConfigException("Failed to deserialize json.", exception);
            }
        }

        public bool Contains(string item)
        {
            return Jsons.Contains(item);
        }

        public int IndexOf(string item)
        {
            return Jsons.IndexOf(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Jsons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Insert(int index, string item)
        {
            Jsons.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Jsons.RemoveAt(index);
        }

        public void Add(string item)
        {
            Jsons.Add(item);
        }

        public void Clear()
        {
            Jsons.Clear();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            Jsons.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            return Jsons.Remove(item);
        }

        /// <summary>
        /// Deserializes the config value for the given key.
        /// </summary>
        /// <exception cref="ConfigKeyNotFoundException">Key does not exist.</exception>
        /// <exception cref="ConfigException">Value could not be deserialized</exception>
        public TItemType GetCheckedValue<TItemType, TConfigType>(params string[] keys)
        {
            TConfigType config = default(TConfigType);
            bool notfoundexception = false;
            foreach (string item in Jsons)
            {
                try
                {
                    config = JsonConvert.DeserializeObject<TConfigType>(item);
                }
                catch
                {
                    continue;
                }

                object field = config;
                Type type = typeof(TConfigType);

                foreach (string item2 in keys)
                {
                    try
                    {
                        FieldInfo fieldInfo = type.GetField(item2,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        if (fieldInfo == null || fieldInfo.GetValue(field) == null)
                        {
                            notfoundexception = true;
                            break;
                        }

                        field = fieldInfo.GetValue(field);
                        type = field.GetType();
                        notfoundexception = false;
                    }
                    catch
                    {
                        notfoundexception = true;
                        break;
                    }
                }

                if (notfoundexception)
                    continue;

                try
                {
                    return (TItemType) field;
                }
                catch (InvalidCastException)
                {
                    throw new ConfigException(
                        $"The desired item: {string.Join(".", keys)}, is not of type {typeof(TItemType)}.");
                }
            }
            if (notfoundexception)
                throw new ConfigKeyNotFoundException($"The config key: {string.Join(".", keys)} could not be found in any json");
            if (config == null || config.Equals(default(TConfigType)))
                throw new ConfigException($"Could not convert any json to the supplied type, list: {string.Join(".", Jsons)}");

            return default(TItemType);
        }

        /// <summary>
        /// Deserializes the config value for the given key and returns
        /// the given default value if key is not found.
        /// </summary>
        public TItemType GetCheckedValueOrDefault<TItemType, TConfigType>(string[] keys, TItemType defaultValue)
        {
            try
            {
                return GetCheckedValue<TItemType, TConfigType>(keys);
            }
            catch (Exception ex) when (ex is ConfigKeyNotFoundException || ex is ConfigException)
            {
                return defaultValue;
            }
        }
    }
}
