using System.Collections.Generic;
using Newtonsoft.Json;

namespace TPP.Core.Configuration;

/// <summary>
/// Base class all configuration classes need to inherit from.
/// </summary>
public abstract class ConfigBase
{
    /* catch all dead config entries to be able to warn the used about those */
    [JsonIgnore]
    [JsonExtensionData]
    public Dictionary<string, object> UnrecognizedConfigs { get; init; } = new Dictionary<string, object>();
}
