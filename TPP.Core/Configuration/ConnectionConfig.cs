using System;
using System.Collections.Immutable;
using System.Linq;
using JsonSubTypes;
using Newtonsoft.Json;

namespace TPP.Core.Configuration
{
    [JsonConverter(typeof(JsonSubtypes), nameof(Type))]
    [JsonSubtypes.KnownSubTypeAttribute(typeof(Console), "console")]
    [JsonSubtypes.KnownSubTypeAttribute(typeof(Twitch), "twitch")]
    [JsonSubtypes.KnownSubTypeAttribute(typeof(Simulation), "simulation")]
    public abstract class ConnectionConfig : ConfigBase
    {
        public abstract string Type { get; init; }
        public abstract string Name { get; init; }

        public sealed class Console : ConnectionConfig
        {
            public override string Type { get; init; } = "console";
            public override string Name { get; init; } = "console-1";

            public string Username { get; init; } = "admin";
        }

        public sealed class Twitch : ConnectionConfig
        {
            public override string Type { get; init; } = "twitch";
            public override string Name { get; init; } = "twitch-1";

            /* connection information */
            public string Channel { get; init; } = "twitchplayspokemon";

            /* account information */
            public string UserId { get; init; } = "1234567";
            public string Username { get; init; } = "justinfan27365461784";
            public string Password { get; init; } = "oauth:mysecret";
            public string UserClientId { get; init; } = "myuserclientid";
            // the access token gets created dynamically from the refresh token
            public string RefreshToken { get; init; } = "myrefreshtoken";
            public string AppClientId { get; init; } = "myappclientid";
            public string AppClientSecret { get; init; } = "myappclientsecret";

            /* communication settings */
            public enum SuppressionType { Whisper, Message, Command }
            public ImmutableHashSet<SuppressionType> Suppressions { get; init; } = Enum
                .GetValues(typeof(SuppressionType))
                .Cast<SuppressionType>()
                .ToImmutableHashSet(); // all by default
            // list of usernames and channels that may receive outbound messages even with suppression enabled
            public ImmutableHashSet<string> SuppressionOverrides { get; init; } = ImmutableHashSet.Create<string>();
        }

        public sealed class Simulation : ConnectionConfig
        {
            public override string Type { get; init; } = "simulation";
            public override string Name { get; init; } = "simulation-1";

            public double InputsPerSecond { get; init; } = 5;
        }
    }
}
