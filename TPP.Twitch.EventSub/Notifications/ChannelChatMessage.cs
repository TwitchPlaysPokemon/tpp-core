using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelChatMessage;
public class ChannelChatMessage(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.chat.message";
    public static string SubscriptionVersion => "1";

    public record Condition(
        string BroadcasterUserId,
        string UserId) : EventSub.Condition;

    public record Message(
        string Text // TODO there is more
    );

    public record Event(
        string BroadcasterUserId,
        string BroadcasterUserName,
        string BroadcasterUserLogin,
        string ChatterUserId,
        string ChatterUserName,
        string ChatterUserLogin,
        string MessageId,
        Message Message,
        string MessageType) : EventSub.Event; // TODO there is more
}
