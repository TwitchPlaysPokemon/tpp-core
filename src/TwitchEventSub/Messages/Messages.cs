namespace TwitchEventSub.Messages;

public class SessionKeepalive(Metadata metadata, SessionKeepalive.KeepalivePayload payload)
    : Message<Metadata, SessionKeepalive.KeepalivePayload>(metadata, payload), IHasMessageType
{
    public static string MessageType => "session_keepalive";

    public record KeepalivePayload : Payload;
}

public class SessionWelcome(Metadata metadata, SessionWelcome.WelcomePayload payload)
    : Message<Metadata, SessionWelcome.WelcomePayload>(metadata, payload), IHasMessageType
{
    public static string MessageType => "session_welcome";

    public record WelcomePayload(Session Session) : Payload;
}

public class SessionReconnect(Metadata metadata, SessionReconnect.ReconnectPayload payload)
    : Message<Metadata, SessionReconnect.ReconnectPayload>(metadata, payload), IHasMessageType
{
    public static string MessageType => "session_reconnect";

    public record ReconnectPayload(Session Session) : Payload;
}

public class Revocation(NotificationMetadata metadata, Revocation.RevocationPayload payload)
    : Message<NotificationMetadata, Revocation.RevocationPayload>(metadata, payload), IHasMessageType
{
    public static string MessageType => "revocation";

    // TODO implement revocation conditions somehow. Right now they are just not deserialized.
    public record RevocationPayload(Subscription<Condition> Subscription) : Payload;
}

public abstract class Notification<C, E>(NotificationMetadata metadata, NotificationPayload<C, E> payload)
    : Message<NotificationMetadata, NotificationPayload<C, E>>(metadata, payload), INotification
    where C : Condition
    where E : Event
{
    NotificationMetadata INotification.Metadata => Metadata;
    Payload INotification.Payload => Payload;
}
