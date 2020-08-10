using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Microsoft.Extensions.Logging;

namespace Core
{
    public static class AlternativePokedexParsers
    {

        struct PokedexEntry
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public static void SetUpPokemonData3(ILogger logger, string pokedexPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            List<PokedexEntry> entries = JsonSerializer.Deserialize<List<PokedexEntry>>(File.ReadAllText(pokedexPath))
                ?? throw new ArgumentException("pokedex data was null");
            foreach (PokedexEntry entry in entries)
            {
                PkmnSpecies.RegisterName(entry.Id, entry.Name);
            }

            stopwatch.Stop();
            Console.WriteLine($"registered {entries.Count} pokedex entries in {stopwatch.ElapsedMilliseconds}ms");
            logger.LogInformation($"registered {entries.Count} pokedex entries in {stopwatch.ElapsedMilliseconds}ms");
        }

        public static void SetUpPokemonData2(ILogger logger, string pokedexPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            using var stream = new FileStream(pokedexPath, FileMode.Open, FileAccess.Read);
            using var jsonStreamReader = new Utf8JsonStreamReader(stream, 32 * 1024);

            Debug.Assert(jsonStreamReader.TokenType == JsonTokenType.None);
            jsonStreamReader.Read();
            Debug.Assert(jsonStreamReader.TokenType == JsonTokenType.StartArray);
            jsonStreamReader.Read();

            int numAdded = 0;
            int numTotal = 0;
            while (jsonStreamReader.TokenType != JsonTokenType.EndArray)
            {
                numTotal++;
                Debug.Assert(jsonStreamReader.TokenType == JsonTokenType.StartObject);
                jsonStreamReader.Read();

                string? id = null;
                string? name = null;
                while (jsonStreamReader.TokenType != JsonTokenType.EndObject)
                {
                    Debug.Assert(jsonStreamReader.TokenType == JsonTokenType.PropertyName);
                    string? key = jsonStreamReader.GetString();
                    jsonStreamReader.Read();
                    if (key == "id") id = jsonStreamReader.GetString();
                    else if (key == "name") name = jsonStreamReader.GetString();
                    else if (jsonStreamReader.TokenType == JsonTokenType.StartArray ||
                             jsonStreamReader.TokenType == JsonTokenType.StartObject)
                    {
                        int originalDepth = jsonStreamReader.CurrentDepth;
                        jsonStreamReader.Read(); // enter nested structure
                        while (jsonStreamReader.CurrentDepth > originalDepth) jsonStreamReader.Read();
                    }
                    jsonStreamReader.Read(); // move to start of next token
                }
                Debug.Assert(jsonStreamReader.TokenType == JsonTokenType.EndObject);
                jsonStreamReader.Read();

                if (id == null)
                {
                    logger.LogError($"entry #{numTotal} in the list has no id");
                    continue;
                }
                if (name == null)
                {
                    logger.LogError($"entry #{numTotal} in the list has no name");
                    continue;
                }

                PkmnSpecies.RegisterName(id, name);
                numAdded++;
            }

            stopwatch.Stop();
            logger.LogInformation($"registered {numAdded} pokedex entries in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
