using NodaTime;

namespace TPP.Twitch.EventSub;

public record Metadata(
    string MessageId,
    string MessageType,
    Instant MessageTimestamp);

public record Payload;

public record NotificationMetadata(
    string MessageId,
    string MessageType,
    Instant MessageTimestamp,
    string SubscriptionType,
    string SubscriptionVersion)
    : Metadata(MessageId, MessageType, MessageTimestamp);

public record Session(
    string Id,
    string Status,
    int? KeepaliveTimeoutSeconds,
    string? ReconnectUrl,
    Instant ConnectedAt);

public record Transport(
    string Method,
    string SessionId);

public record Condition;
public record Event;

public record Subscription<C>(
    string Id,
    string Status,
    string Type,
    string Version,
    int Cost,
    C Condition,
    Transport Transport,
    Instant CreatedAt) where C : Condition;

public record NotificationPayload<C, E>(Subscription<C> Subscription, E Event) : Payload
    where C : Condition;
