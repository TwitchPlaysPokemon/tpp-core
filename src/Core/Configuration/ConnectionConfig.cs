using System;
using System.Collections.Immutable;
using System.Linq;
using JsonSubTypes;
using Newtonsoft.Json;
using NodaTime;
using Core.Utils;

namespace Core.Configuration;

[JsonConverter(typeof(JsonSubtypes), nameof(Type))]
[JsonSubtypes.KnownSubType(typeof(Console), "console")]
[JsonSubtypes.KnownSubType(typeof(Twitch), "twitch")]
[JsonSubtypes.KnownSubType(typeof(Simulation), "simulation")]
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
        public string ChannelId { get; init; } = "56648155";

        /* account information */
        public string UserId { get; init; } = "1234567";
        public string Username { get; init; } = "justinfan27365461784";
        public string Password { get; init; } = "oauth:mysecret";
        // if an infinite access token is specified, assumes it has infinite validity and always uses that one
        public string? InfiniteAccessToken { get; init; } = null;
        // if no access token is specified, dynamically create access tokens from this refresh token
        public string? RefreshToken { get; init; } = "myrefreshtoken";

        // Same as above, but for channel scopes (e.g. "TwitchPlaysPokemon" instead of "tpp")
        public string? ChannelInfiniteAccessToken { get; init; } = null;
        public string? ChannelRefreshToken { get; init; } = null;

        public string AppClientId { get; init; } = "myappclientid";
        public string AppClientSecret { get; init; } = "myappclientsecret";

        // If this Twitch chat connection should get monitored for subscriptions.
        // Only enable this for the "main" channel, otherwise subs in other channels would e.g. give tokens too.
        public bool MonitorSubscriptions { get; init; } = true;

        // Whether people can do !join in chat for tpp to join their channel, and consume inputs from there.
        public bool CoStreamInputsEnabled { get; init; } = false;
        // Whether the co-stream-inputs feature only allows !join for people currently streaming.
        public bool CoStreamInputsOnlyLive { get; init; } = true;

        /* communication settings */
        public enum SuppressionType { Whisper, Message, Command }
        public ImmutableHashSet<SuppressionType> Suppressions { get; init; } = Enum
            .GetValues(typeof(SuppressionType))
            .Cast<SuppressionType>()
            .ToImmutableHashSet(); // all by default
        // list of usernames and channels that may receive outbound messages even with suppression enabled
        public CaseInsensitiveImmutableHashSet SuppressionOverrides { get; init; } = new([]);

        public Duration? GetChattersInterval { get; init; } = Duration.FromMinutes(5);
    }

    public sealed class Simulation : ConnectionConfig
    {
        public override string Type { get; init; } = "simulation";
        public override string Name { get; init; } = "simulation-1";

        public double InputsPerSecond { get; init; } = 5;
    }
}
