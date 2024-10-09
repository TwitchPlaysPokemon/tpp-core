using System.Runtime.Serialization;
using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelSubscribe;

/// <summary>
/// The channel.subscribe subscription type sends a notification when a specified channel receives a subscriber. This does not include resubscribes.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelsubscribe">Subscription Types Documentation for 'Channel Subscribe'</a>
/// </summary>
public class ChannelSubscribe(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.subscribe";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Subscribe Condition
    /// <param name="BroadcasterUserId">The broadcaster user ID for the channel you want to get subscribe notifications for.</param>
    /// </summary>
    public record Condition(string BroadcasterUserId) : EventSub.Condition;

    /// <summary>
    /// The tier of the subscription. Valid values are 1000, 2000, and 3000.
    /// </summary>
    [DataContract] // Custom enum member values, because enums cannot be numbers in code
    public enum Tier
    {
        [EnumMember(Value = "1000")] Tier1000,
        [EnumMember(Value = "2000")] Tier2000,
        [EnumMember(Value = "3000")] Tier3000
    }

    /// <summary>
    /// Channel Subscribe Event
    /// </summary>
    /// <param name="UserId">The user ID for the user who subscribed to the specified channel.</param>
    /// <param name="UserLogin">The user login for the user who subscribed to the specified channel.</param>
    /// <param name="UserName">The user display name for the user who subscribed to the specified channel.</param>
    /// <param name="BroadcasterUserId">The requested broadcaster ID.</param>
    /// <param name="BroadcasterUserLogin">The requested broadcaster login.</param>
    /// <param name="BroadcasterUserName">The requested broadcaster display name.</param>
    /// <param name="Tier">The tier of the subscription. Valid values are 1000, 2000, and 3000.</param>
    /// <param name="IsGift">Whether the subscription is a gift.</param>
    public record Event(
        string UserId,
        string UserLogin,
        string UserName,
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        Tier Tier,
        bool IsGift
    ) : EventSub.Event;
}
