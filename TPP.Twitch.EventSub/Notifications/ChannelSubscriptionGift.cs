using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelSubscriptionGift;

/// <summary>
/// The channel.subscription.gift subscription type sends a notification when a user gives one or more gifted subscriptions in a channel.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelsubscriptiongift">Subscription Types Documentation for 'Channel Subscription Gift'</a>
/// </summary>
public class ChannelSubscriptionGift(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.subscription.gift";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Subscription Gift Condition
    /// <param name="BroadcasterUserId">The broadcaster user ID for the channel you want to get subscription gift notifications for.</param>
    /// </summary>
    public record Condition(string BroadcasterUserId) : EventSub.Condition;

    /// <summary>
    /// Channel Subscription Gift Event
    /// </summary>
    /// <param name="UserId">The user ID of the user who sent the subscription gift. Set to null if it was an anonymous subscription gift.</param>
    /// <param name="UserLogin">The user login of the user who sent the gift. Set to null if it was an anonymous subscription gift.</param>
    /// <param name="UserName">The user display name of the user who sent the gift. Set to null if it was an anonymous subscription gift.</param>
    /// <param name="BroadcasterUserId">The broadcaster user ID.</param>
    /// <param name="BroadcasterUserLogin">The broadcaster login.</param>
    /// <param name="BroadcasterUserName">The broadcaster display name.</param>
    /// <param name="Total">The number of subscriptions in the subscription gift.</param>
    /// <param name="Tier">The tier of the subscription. Valid values are 1000, 2000, and 3000.</param>
    /// <param name="CumulativeTotal">The number of subscriptions gifted by this user in the channel. This value is null for anonymous gifts or if the gifter has opted out of sharing this information.</param>
    /// <param name="IsAnonymous">Whether the subscription gift was anonymous.</param>
    public record Event(
        string? UserId,
        string? UserLogin,
        string? UserName,
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        int Total,
        ChannelSubscribe.Tier Tier,
        int? CumulativeTotal,
        bool IsAnonymous
    ) : EventSub.Event;
}
