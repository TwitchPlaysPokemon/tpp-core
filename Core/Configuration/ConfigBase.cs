using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Configuration
{
    /// <summary>
    /// Base class all configuration classes need to inherit from.
    /// </summary>
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public abstract class ConfigBase
    {
        /* catch all dead config entries to be able to warn the used about those */
        [JsonIgnore]
        [JsonExtensionData]
        public Dictionary<string, object> UnrecognizedConfigs { get; private set; } = new Dictionary<string, object>();
    }
}
