using NodaTime;
using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelChannelPointsCustomRewardRedemptionUpdate;

/// <summary>
/// The channel.channel_points_custom_reward_redemption.update subscription type sends a notification when a redemption of a channel points custom reward has been updated for the specified channel.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchannel_points_custom_reward_redemptionupdate">Subscription Types Documentation for 'Channel Points Custom Reward Redemption Update'</a>
/// </summary>
public class ChannelChannelPointsCustomRewardRedemptionUpdate(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.channel_points_custom_reward_redemption.update";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Points Custom Reward Redemption Update Condition
    /// <param name="BroadcasterUserId">The broadcaster user ID for the channel you want to receive channel points custom reward redemption update notifications for.</param>
    /// <param name="RewardId">Optional. Specify a reward id to only receive notifications for a specific reward.</param>
    /// </summary>
    public record Condition(string BroadcasterUserId, string? RewardId = null) : EventSub.Condition;

    /// <summary>
    /// Channel Points Custom Reward Redemption Update Event
    /// </summary>
    /// <param name="Id">The redemption identifier.</param>
    /// <param name="BroadcasterUserId">The requested broadcaster user ID.</param>
    /// <param name="BroadcasterUserLogin">The requested broadcaster login.</param>
    /// <param name="BroadcasterUserName">The requested broadcaster display name.</param>
    /// <param name="UserId">User ID of the user that redeemed the reward.</param>
    /// <param name="UserLogin">Login of the user that redeemed the reward.</param>
    /// <param name="UserName">Display name of the user that redeemed the reward.</param>
    /// <param name="UserInput">The user input provided. Empty string if not provided.</param>
    /// <param name="Status">Will be fulfilled or canceled. Possible values are unknown, unfulfilled, fulfilled, and canceled.</param>
    /// <param name="Reward">Basic information about the reward that was redeemed, at the time it was redeemed.</param>
    /// <param name="RedeemedAt">RFC3339 timestamp of when the reward was redeemed.</param>
    public record Event(
        string Id,
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        string UserId,
        string UserLogin,
        string UserName,
        string UserInput,
        RedemptionStatus Status,
        RedemptionReward Reward,
        Instant RedeemedAt
    ) : EventSub.Event;
}
