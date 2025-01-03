using System.Linq;
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
    /// Some additional gotchas that are not mentioned in the official documentation:
    /// - The positions are counted in what appears to be UTF-8 bytes
    /// - The begin is 0-based and inclusive (unsurprising), but the end is also inclusive (kinda surprising).
    /// An experiment showed that this string:
    /// <code>test Kappa 🌿 BabyRage 🐍 PogChamp 🎅🏿 RaccAttack ♥ PraiseIt 12345678901234567890</code>
    /// resulted in these emote indices:
    /// <code>
    /// {"begin": 5, "end": 9, "id": "25"},
    /// {"begin": 16, "end": 23, "id": "22639"},
    /// {"begin": 30, "end": 37, "id": "305954156"},
    /// {"begin": 48, "end": 57, "id": "114870"},
    /// {"begin": 63, "end": 70, "id": "38586"}
    /// </code>
    public record Emote(
        int Begin,
        int End,
        string Id);

    /// <summary>
    /// An object that contains the resubscription message and emote information needed to recreate the message.
    /// </summary>
    /// <param name="Text">The text of the resubscription chat message.
    /// This might not be nullable, but since the documentation sometimes forgets to note possible nulls, better be safe.</param>
    /// <param name="Emotes">An array that includes the emote ID and start and end positions for where the emote appears in the text.
    /// Although the documentation doesn't say it explicitly, it is nullable.</param>
    public record Message(string? Text, Emote[]? Emotes)
    {
        // override Equals and GetHashCode to fix value semantics for collections: Emotes
        public virtual bool Equals(Message? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text == other.Text && (Emotes == null
                ? other.Emotes == null
                : other.Emotes != null && Emotes.SequenceEqual(other.Emotes));
        }
        public override int GetHashCode() => Text?.GetHashCode() ?? 0;

        // default ToString of arrays is useless :( I don't want "Emote[]", I want the actual emotes!
        public override string ToString()
        {
            return $"Message {{ {nameof(Text)} = <{Text}>, {nameof(Emotes)} = [ "
                   + string.Join(", ", (Emotes ?? []).Select(x => x.ToString())) + " ] }";
        }
    }

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
    /// <param name="Message">An object that contains the resubscription message and emote information needed to recreate the message.
    /// This might not be nullable, but since the documentation sometimes forgets to note possible nulls, better be safe.</param>
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
        ChannelSubscribe.Tier Tier,
        Message? Message,
        int CumulativeMonths,
        int? StreakMonths,
        int DurationMonths
    ) : EventSub.Event;
}
