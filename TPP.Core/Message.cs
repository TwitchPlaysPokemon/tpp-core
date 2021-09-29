using System.Collections.Immutable;
using TPP.Model;

namespace TPP.Core;

public enum MessageSource
{
    Chat,
    Whisper,
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
