using TPP.Twitch.EventSub.Messages;

namespace TPP.Twitch.EventSub.Notifications;

using static ChannelSubscriptionMessage;

/// <summary>
/// The channel.subscription.message subscription type sends a notification when a user sends a resubscription chat message in a specific channel.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelsubscriptionmessage">Subscription Types Documentation for 'Channel Subscription Message'</a>
/// </summary>
public class ChannelSubscriptionMessage(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.subscription.message";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Subscription Message Condition
    /// <param name="BroadcasterUserId">The broadcaster user ID for the channel you want to get resubscription chat message notifications for.</param>
    /// </summary>
    public record Condition(string BroadcasterUserId) : EventSub.Condition;

    /// <summary>
    /// An array that includes the emote ID and start and end positions for where the emote appears in the text.
    /// </summary>
    /// <param name="Begin">The index of where the Emote starts in the text.</param>
    /// <param name="End">The index of where the Emote ends in the text.</param>
    /// <param name="Id">The emote ID.</param>
    public record Emote(
        int Begin,
        int End,
        string Id);

    /// <summary>
    /// An object that contains the resubscription message and emote information needed to recreate the message.
    /// </summary>
    /// <param name="Text">The text of the resubscription chat message.</param>
    /// <param name="Emotes">An array that includes the emote ID and start and end positions for where the emote appears in the text.</param>
    public record Message(
        string Text,
        Emote[] Emotes);

    /// <summary>
    /// Channel Subscription Message Event
    /// </summary>
    /// <param name="UserId">The user ID of the user who sent a resubscription chat message.</param>
    /// <param name="UserLogin">The user login of the user who sent a resubscription chat message.</param>
    /// <param name="UserName">The user display name of the user who a resubscription chat message.</param>
    /// <param name="BroadcasterUserId">The broadcaster user ID.</param>
    /// <param name="BroadcasterUserLogin">The broadcaster login.</param>
    /// <param name="BroadcasterUserName">The broadcaster display name.</param>
    /// <param name="Tier">The tier of the user’s subscription.</param>
    /// <param name="Message">An object that contains the resubscription message and emote information needed to recreate the message.</param>
    /// <param name="CumulativeMonths">The total number of months the user has been subscribed to the channel.</param>
    /// <param name="StreakMonths">The number of consecutive months the user’s current subscription has been active. This value is null if the user has opted out of sharing this information. null if not shared.</param>
    /// <param name="DurationMonths">The month duration of the subscription.</param>
    public record Event(
        string UserId,
        string UserLogin,
        string UserName,
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        string Tier,
        Message Message,
        int CumulativeMonths,
        int? StreakMonths,
        int DurationMonths
    ) : EventSub.Event;
}
