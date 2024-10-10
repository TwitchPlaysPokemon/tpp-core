using EventSub.Messages;

namespace EventSub.Notifications;

using static UserWhisperMessage;

/// <summary>
/// The user.whisper.message subscription type sends a notification when a user receives a whisper. Event Triggers - Anyone whispers the specified user.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#userwhispermessage">Subscription Types Documentation for 'Whisper Received'</a>
/// </summary>
public class UserWhisperMessage(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "user.whisper.message";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Whisper Received Condition
    /// </summary>
    /// <param name="UserId">The user_id of the person receiving whispers.</param>
    public record Condition(string UserId) : EventSub.Condition;

    /// <summary>
    /// Object containing whisper information.
    /// </summary>
    /// <param name="Text">The body of the whisper message.</param>
    public record Whisper(string Text);

    /// <summary>
    /// Whisper Received Event
    /// </summary>
    /// <param name="FromUserId">The ID of the user sending the message.</param>
    /// <param name="FromUserName">The name of the user sending the message.</param>
    /// <param name="FromUserLogin">The login of the user sending the message.</param>
    /// <param name="ToUserId">The ID of the user receiving the message.</param>
    /// <param name="ToUserName">The name of the user receiving the message.</param>
    /// <param name="ToUserLogin">The login of the user receiving the message.</param>
    /// <param name="WhisperId">The whisper ID.</param>
    /// <param name="Whisper">Object containing whisper information.</param>
    public record Event(
        string FromUserId,
        string FromUserName,
        string FromUserLogin,
        string ToUserId,
        string ToUserName,
        string ToUserLogin,
        string WhisperId,
        Whisper Whisper) : EventSub.Event;
}
