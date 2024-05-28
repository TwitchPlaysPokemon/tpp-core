using Newtonsoft.Json;

namespace TPP.Core.Configuration;

/// <summary>
/// Interface for configs that are _not_ just part of another configuration,
/// but a root config intended to be read from and written to a file on disk.
/// </summary>
public interface IRootConfig
{
    public const string SchemaFieldName = "$schema";
    [JsonProperty(SchemaFieldName)] public string Schema { get; }
}
