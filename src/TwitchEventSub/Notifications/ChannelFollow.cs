using NodaTime;
using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelFollow;

/// <summary>
/// The channel.follow subscription type sends a notification when a specified channel receives a follow.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelfollow">Subscription Types Documentation for 'Channel Follow'</a>
/// </summary>
public class ChannelFollow(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.follow";
    public static string SubscriptionVersion => "2";

    /// <summary>
    /// Channel Follow Condition
    /// </summary>
    /// <param name="BroadcasterUserId">The broadcaster user ID for the channel you want to get follow notifications for.</param>
    /// <param name="ModeratorUserId">The ID of the moderator of the channel you want to get follow notifications for. If you have authorization from the broadcaster rather than a moderator, specify the broadcaster’s user ID here.</param>
    public record Condition(
        string BroadcasterUserId,
        string ModeratorUserId) : EventSub.Condition;

    /// <summary>
    /// Channel Follow Event
    /// </summary>
    /// <param name="UserId">The user ID for the user now following the specified channel.</param>
    /// <param name="UserLogin">The user login for the user now following the specified channel.</param>
    /// <param name="UserName">The user display name for the user now following the specified channel.</param>
    /// <param name="BroadcasterUserId">The requested broadcaster ID.</param>
    /// <param name="BroadcasterUserLogin">The requested broadcaster login.</param>
    /// <param name="BroadcasterUserName">The requested broadcaster display name.</param>
    /// <param name="FollowedAt">RFC3339 timestamp of when the follow occurred.</param>
    public record Event(
        string UserId,
        string UserLogin,
        string UserName,
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        Instant FollowedAt) : EventSub.Event;
}
