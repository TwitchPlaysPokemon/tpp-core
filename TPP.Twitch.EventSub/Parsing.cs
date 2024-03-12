using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime.Serialization.SystemTextJson;
using TPP.Twitch.EventSub.Messages;
using TPP.Twitch.EventSub.Notifications;

namespace TPP.Twitch.EventSub;

public static class Parsing
{
    /// <summary>
    /// By default all known message types (at the time of authorship) are known to this facility.
    /// But if you need to add a custom message type, call this function.
    /// Calling this may overwrite a previous message type, if the new one has the same message type string
    /// (as defined by <see cref="IHasMessageType"/>) as an existing one.
    /// </summary>
    public static void RegisterMessageType<T>() where T : IHasMessageType, IMessage =>
        MessageTypes[T.MessageType] = typeof(T);
    /// <summary>
    /// By default all known subscription types (at the time of authorship) are known to this facility.
    /// But if you need to add a custom subscription type, call this function.
    /// Calling this may overwrite a previous subscription type, if the new one has the same subscription type
    /// and version string (as defined by <see cref="IHasSubscriptionType"/>) as an existing one.
    /// </summary>
    public static void RegisterSubscriptionType<T>() where T : IHasSubscriptionType, INotification =>
        SubscriptionTypes[T.SubscriptionType] = typeof(T);

    static Parsing()
    {
        RegisterMessageType<SessionKeepalive>();
        RegisterMessageType<SessionWelcome>();
        RegisterMessageType<SessionReconnect>();
        RegisterMessageType<Revocation>();
        // The "notification" message type is implicitly known
        RegisterSubscriptionType<ChannelFollow>();
        RegisterSubscriptionType<ChannelChatMessage>();
        RegisterSubscriptionType<ChannelChatSettingsUpdate>();
        RegisterSubscriptionType<ChannelSubscribe>();
        RegisterSubscriptionType<ChannelSubscriptionMessage>();
    }

    private static readonly Dictionary<string, Type> MessageTypes = new();
    private static readonly Dictionary<string, Type> SubscriptionTypes = new();

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            NodaConverters.InstantConverter,
            new ChannelSubscribe.TierConverter(), // see ChannelSubscribe#Tier
            new JsonStringEnumConverter(),
        },
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public record ParseResult
    {
        private ParseResult()
        {
            // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
            // closed set, simulating a sum type.
        }

        public sealed record Ok(IMessage Message) : ParseResult;
        public sealed record InvalidMessage(string Error) : ParseResult;
        public sealed record UnknownMessageType(string MessageType) : ParseResult;
        public sealed record UnknownSubscriptionType(string SubscriptionType) : ParseResult;
    }

    public static ParseResult Parse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("metadata", out JsonElement metadata))
                return new ParseResult.InvalidMessage("missing metadata");
            if (!metadata.TryGetProperty("message_type", out JsonElement messageTypeElem))
                return new ParseResult.InvalidMessage("missing message type");
            if (messageTypeElem.ValueKind != JsonValueKind.String ||
                messageTypeElem.GetString() is not { } messageTypeStr)
                return new ParseResult.InvalidMessage("no valid message type, must be not-null string");

            if (messageTypeStr == "notification")
            {
                if (!metadata.TryGetProperty("subscription_type", out JsonElement subTypeElem))
                    return new ParseResult.InvalidMessage("missing subscription type");
                if (subTypeElem.ValueKind != JsonValueKind.String || subTypeElem.GetString() is not { } subTypeStr)
                    return new ParseResult.InvalidMessage("no valid subscription type, must be not-null string");
                if (!SubscriptionTypes.TryGetValue(subTypeStr, out Type? subType))
                    return new ParseResult.UnknownSubscriptionType(subTypeStr);
                var subMessage = JsonSerializer.Deserialize(json, subType, SerializerOptions) as INotification;
                return new ParseResult.Ok(subMessage ?? throw new ArgumentException(
                    $"subscription type {subType} unexpectedly null or not of type {typeof(INotification)}"));
            }

            if (!MessageTypes.TryGetValue(messageTypeStr, out Type? messageType))
                return new ParseResult.UnknownMessageType(messageTypeStr);
            var message = JsonSerializer.Deserialize(json, messageType, SerializerOptions) as IMessage;
            return new ParseResult.Ok(message ?? throw new ArgumentException(
                $"subscription type {messageType} unexpectedly null or not of type {typeof(IMessage)}"));
        }
        catch (JsonException ex)
        {
            return new ParseResult.InvalidMessage("Invalid json: <" + json + ">, Error: " + ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return new ParseResult.InvalidMessage(
                "Deserialization of json failed due to missing support: <" + json + ">, Error: " + ex.Message);
        }
    }
}

public interface IHasMessageType
{
    public static abstract string MessageType { get; }
}

public interface IHasSubscriptionType
{
    public static abstract string SubscriptionType { get; }
    public static abstract string SubscriptionVersion { get; }
}

public interface IMessage
{
    public Metadata Metadata { get; }
    public Payload Payload { get; }
}

public interface INotification : IMessage, IHasMessageType
{
    static string IHasMessageType.MessageType => "notification";

    public new NotificationMetadata Metadata { get; }
    public new Payload Payload { get; }
}

public abstract class Message<M, P>(M metadata, P payload) : IMessage
    where M : Metadata
    where P : Payload
{
    public M Metadata { get; } = metadata;
    public P Payload { get; } = payload;

    Metadata IMessage.Metadata => Metadata;
    Payload IMessage.Payload => Payload;
}
