using EventSub.Messages;

namespace EventSub.Notifications;

using static ChannelChatSettingsUpdate;

/// <summary>
/// This event sends a notification when a broadcaster’s chat settings are updated.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchat_settingsupdate">Subscription Types Documentation for 'Channel Chat Settings Update'</a>
/// </summary>
public class ChannelChatSettingsUpdate(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.chat_settings.update";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Chat Settings Update Condition
    /// </summary>
    /// <param name="BroadcasterUserId">User ID of the channel to receive chat settings update events for.</param>
    /// <param name="UserId">The user ID to read chat as.</param>
    public record Condition(
        string BroadcasterUserId,
        string UserId
    ) : EventSub.Condition;

    /// <summary>
    /// Channel Chat Settings Update Event
    /// </summary>
    /// <param name="BroadcasterUserId">The ID of the broadcaster specified in the request.</param>
    /// <param name="BroadcasterUserLogin">The login of the broadcaster specified in the request.</param>
    /// <param name="BroadcasterUserName">The user name of the broadcaster specified in the request.</param>
    /// <param name="EmoteMode">A Boolean value that determines whether chat messages must contain only emotes. True if only messages that are 100% emotes are allowed; otherwise false.</param>
    /// <param name="FollowerMode">A Boolean value that determines whether the broadcaster restricts the chat room to followers only, based on how long they’ve followed.
    /// True if the broadcaster restricts the chat room to followers only; otherwise false.
    /// See follower_mode_duration_minutes for how long the followers must have followed the broadcaster to participate in the chat room.</param>
    /// <param name="FollowerModeDurationMinutes">The length of time, in minutes, that the followers must have followed the broadcaster to participate in the chat room. See follower_mode.
    /// Null if follower_mode is false.</param>
    /// <param name="SlowMode">A Boolean value that determines whether the broadcaster limits how often users in the chat room are allowed to send messages.
    /// Is true, if the broadcaster applies a delay; otherwise, false.
    /// See slow_mode_wait_time_seconds for the delay.</param>
    /// <param name="SlowModeWaitTimeSeconds">The amount of time, in seconds, that users need to wait between sending messages. See slow_mode.
    /// Null if slow_mode is false.</param>
    /// <param name="SubscriberMode">A Boolean value that determines whether only users that subscribe to the broadcaster’s channel can talk in the chat room.
    /// True if the broadcaster restricts the chat room to subscribers only; otherwise false.</param>
    /// <param name="UniqueChatMode">A Boolean value that determines whether the broadcaster requires users to post only unique messages in the chat room.
    /// True if the broadcaster requires unique messages only; otherwise false.</param>
    public record Event(
        string BroadcasterUserId,
        string BroadcasterUserLogin,
        string BroadcasterUserName,
        bool EmoteMode,
        bool FollowerMode,
        int? FollowerModeDurationMinutes,
        bool SlowMode,
        int? SlowModeWaitTimeSeconds,
        bool SubscriberMode,
        bool UniqueChatMode
    ) : EventSub.Event;
}
