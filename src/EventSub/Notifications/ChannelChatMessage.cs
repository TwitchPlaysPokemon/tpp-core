using System;
using System.Collections.Immutable;
using System.Linq;
using EventSub.Messages;

namespace EventSub.Notifications;

using static ChannelChatMessage;

/// <summary>
/// The channel.chat.message subscription type sends a notification when any user sends a message to a specific chat room.
/// See <a href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage">Subscription Types Documentation for 'Channel Chat Message'</a>
/// </summary>
public class ChannelChatMessage(NotificationMetadata metadata, NotificationPayload<Condition, Event> payload)
    : Notification<Condition, Event>(metadata, payload), IHasSubscriptionType
{
    public static string SubscriptionType => "channel.chat.message";
    public static string SubscriptionVersion => "1";

    /// <summary>
    /// Channel Chat Message Condition
    /// </summary>
    /// <param name="BroadcasterUserId">User ID of the channel to receive chat message events for.</param>
    /// <param name="UserId">The user ID to read chat as.</param>
    public record Condition(
        string BroadcasterUserId,
        string UserId
    ) : EventSub.Condition;

    /// <param name="Prefix">The name portion of the Cheermote string that you use in chat to cheer Bits. The full Cheermote string is the concatenation of {prefix} + {number of Bits}. For example, if the prefix is “Cheer” and you want to cheer 100 Bits, the full Cheermote string is Cheer100. When the Cheermote string is entered in chat, Twitch converts it to the image associated with the Bits tier that was cheered.</param>
    /// <param name="Bits">The amount of bits cheered.</param>
    /// <param name="Tier">The tier level of the cheermote.</param>
    public record Cheermote(
        string Prefix,
        int Bits,
        int Tier
    );

    public enum EmoteFormat
    {
        /// An animated GIF is available for this emote.
        Animated,
        /// A static PNG file is available for this emote.
        Static
    }

    /// <param name="Id">An ID that uniquely identifies this emote.</param>
    /// <param name="EmoteSetId">An ID that identifies the emote set that the emote belongs to.</param>
    /// <param name="OwnerId">The ID of the broadcaster who owns the emote.</param>
    /// <param name="Format">The formats that the emote is available in. For example, if the emote is available only as a static PNG, the array contains only static. But if the emote is available as a static PNG and an animated GIF, the array contains static and animated. The possible formats are: animated, static</param>
    public record Emote(
        string Id,
        string EmoteSetId,
        string OwnerId,
        IImmutableSet<EmoteFormat> Format
    )
    {
        public override string ToString() =>
            $"{nameof(Emote)} {{ " +
            $"{nameof(Id)} = {Id}, " +
            $"{nameof(EmoteSetId)} = {EmoteSetId}, " +
            $"{nameof(OwnerId)} = {OwnerId}, " +
            $"{nameof(Format)} = [ {string.Join(", ", Format)} ] " +
            $"}}";
    }

    /// <param name="UserId">The user ID of the mentioned user.</param>
    /// <param name="UserName">The user name of the mentioned user.</param>
    /// <param name="UserLogin">The user login of the mentioned user.</param>
    public record Mention(
        string UserId,
        string UserName,
        string UserLogin
    );

    public enum FragmentType
    {
        Text,
        Cheermote,
        Emote,
        Mention
    }

    /// <param name="Type">The type of message fragment. Possible values: text, cheermote, emote, mention</param>
    /// <param name="Text">Message text in fragment.</param>
    /// <param name="Cheermote">Optional. Metadata pertaining to the cheermote.</param>
    /// <param name="Emote">Optional. Metadata pertaining to the emote.</param>
    /// <param name="Mention">Optional. Metadata pertaining to the mention.</param>
    public record Fragment(
        FragmentType Type,
        string Text,
        Cheermote? Cheermote,
        Emote? Emote,
        Mention? Mention
    );

    /// <summary>
    /// The structured chat message.
    /// </summary>
    /// <param name="Text">The chat message in plain text.</param>
    /// <param name="Fragments">Ordered list of chat message fragments.</param>
    public record Message(
        string Text,
        ImmutableArray<Fragment> Fragments
    )
    {
        public override string ToString() =>
            $"{nameof(Message)} {{ {nameof(Text)} = {Text}, {nameof(Fragments)} = [ {string.Join(", ", Fragments)} ] }}";

        // override Equals and GetHashCode to fix value semantics for collections: Fragments
        public virtual bool Equals(Message? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text == other.Text && Fragments.SequenceEqual(other.Fragments);
        }
        public override int GetHashCode() => Text.GetHashCode();
    }

    /// <param name="SetId">An ID that identifies this set of chat badges. For example, Bits or Subscriber.</param>
    /// <param name="Id">An ID that identifies this version of the badge. The ID can be any value. For example, for Bits, the ID is the Bits tier level, but for World of Warcraft, it could be Alliance or Horde.</param>
    /// <param name="Info">Contains metadata related to the chat badges in the badges tag. Currently, this tag contains metadata only for subscriber badges, to indicate the number of months the user has been a subscriber.</param>
    public record Badge(string SetId, string Id, string Info);

    /// <param name="Bits">The amount of Bits the user cheered.</param>
    public record Cheer(int Bits);

    /// <param name="ParentMessageId">An ID that uniquely identifies the parent message that this message is replying to.</param>
    /// <param name="ParentMessageBody">The message body of the parent message.</param>
    /// <param name="ParentUserId">User ID of the sender of the parent message.</param>
    /// <param name="ParentUserName">User name of the sender of the parent message.</param>
    /// <param name="ParentUserLogin">User login of the sender of the parent message.</param>
    /// <param name="ThreadMessageId">An ID that identifies the parent message of the reply thread.</param>
    /// <param name="ThreadUserId">User ID of the sender of the thread’s parent message.</param>
    /// <param name="ThreadUserName">User name of the sender of the thread’s parent message.</param>
    /// <param name="ThreadUserLogin">User login of the sender of the thread’s parent message.</param>
    public record Reply(
        string ParentMessageId,
        string ParentMessageBody,
        string ParentUserId,
        string ParentUserName,
        string ParentUserLogin,
        string ThreadMessageId,
        string ThreadUserId,
        string ThreadUserName,
        string ThreadUserLogin
    );

    /// <summary>
    /// The type of message. Possible values:
    /// - text
    /// - channel_points_highlighted
    /// - channel_points_sub_only
    /// - user_intro
    /// - power_ups_message_effect
    /// - power_ups_gigantified_emote
    /// </summary>
    public enum MessageType
    {
        Text,
        ChannelPointsHighlighted,
        ChannelPointsSubOnly,
        UserIntro,
        PowerUpsMessageEffect,
        PowerUpsGigantifiedEmote,
    }

    /// <summary>
    /// Channel Chat Message Event
    /// </summary>
    /// <param name="BroadcasterUserId">The broadcaster user ID.</param>
    /// <param name="BroadcasterUserName">The broadcaster display name.</param>
    /// <param name="BroadcasterUserLogin">The broadcaster login.</param>
    /// <param name="ChatterUserId">The user ID of the user that sent the message.</param>
    /// <param name="ChatterUserName">The user name of the user that sent the message.</param>
    /// <param name="ChatterUserLogin">The user login of the user that sent the message.</param>
    /// <param name="MessageId">A UUID that identifies the message.</param>
    /// <param name="Message">The structured chat message.</param>
    /// <param name="MessageType">The type of message. Possible values: text, channel_points_highlighted, channel_points_sub_only, user_intro</param>
    /// <param name="Badges">List of chat badges.</param>
    /// <param name="Cheer">Optional. Metadata if this message is a cheer.</param>
    /// <param name="Color">The color of the user’s name in the chat room. This is a hexadecimal RGB color code in the form, #&lt;RGB&gt;. This tag may be empty if it is never set.</param>
    /// <param name="Reply">Optional. Metadata if this message is a reply.</param>
    /// <param name="ChannelPointsCustomRewardId">Optional. The ID of a channel points custom reward that was redeemed.</param>
    public record Event(
        string BroadcasterUserId,
        string BroadcasterUserName,
        string BroadcasterUserLogin,
        string ChatterUserId,
        string ChatterUserName,
        string ChatterUserLogin,
        string MessageId,
        Message Message,
        MessageType MessageType,
        ImmutableArray<Badge> Badges,
        Cheer? Cheer,
        string Color,
        Reply? Reply,
        string? ChannelPointsCustomRewardId
    ) : EventSub.Event
    {
        // override Equals and GetHashCode to fix value semantics for collections: Badges
        public virtual bool Equals(Event? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && BroadcasterUserId == other.BroadcasterUserId
                   && BroadcasterUserName == other.BroadcasterUserName
                   && BroadcasterUserLogin == other.BroadcasterUserLogin
                   && ChatterUserId == other.ChatterUserId
                   && ChatterUserName == other.ChatterUserName
                   && ChatterUserLogin == other.ChatterUserLogin
                   && MessageId == other.MessageId
                   && Message.Equals(other.Message)
                   && MessageType == other.MessageType
                   && Badges.SequenceEqual(other.Badges) // <-- !!! change from default equals
                   && Equals(Cheer, other.Cheer)
                   && Color == other.Color
                   && Equals(Reply, other.Reply)
                   && ChannelPointsCustomRewardId == other.ChannelPointsCustomRewardId;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(BroadcasterUserId);
            hashCode.Add(BroadcasterUserName);
            hashCode.Add(BroadcasterUserLogin);
            hashCode.Add(ChatterUserId);
            hashCode.Add(ChatterUserName);
            hashCode.Add(ChatterUserLogin);
            hashCode.Add(MessageId);
            hashCode.Add(Message);
            hashCode.Add((int)MessageType);
            // hashCode.Add(Badges); // skip this one to make GetHashCode consistent with Equals
            hashCode.Add(Cheer);
            hashCode.Add(Color);
            hashCode.Add(Reply);
            hashCode.Add(ChannelPointsCustomRewardId);
            return hashCode.ToHashCode();
        }
    }
}
