using System.Collections.Immutable;
using Model;

namespace Core
{
    public record MessageSource
    {
        private MessageSource()
        {
            // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
            // closed set, simulating a sum type.
        }

        public sealed record PrimaryChat : MessageSource;
        public sealed record SecondaryChat(string ChannelId, string ChannelName) : MessageSource;
        public sealed record Whisper : MessageSource;
    }

    public record Emote(string Id, string Name, int StartIndex, int EndIndex);

    public sealed record MessageDetails(
        string? MessageId,
        bool IsAction,
        bool IsStaff,
        IImmutableList<Emote> Emotes);

    public sealed record Message(
        User User,
        string MessageText,
        MessageSource MessageSource,
        string RawIrcMessage)
    {
        public MessageDetails Details { get; init; } =
            new(MessageId: null, IsAction: false, IsStaff: false, Emotes: ImmutableList<Emote>.Empty);
    }
}
