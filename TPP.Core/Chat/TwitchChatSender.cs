using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Configuration;
using TPP.Model;
using static TPP.Core.Configuration.ConnectionConfig.Twitch;

namespace TPP.Core.Chat;

public sealed class TwitchChatSender : IMessageSender, IAsyncDisposable
{
    /// Twitch Messaging Interface (TMI, the somewhat IRC-compatible protocol twitch uses) maximum message length.
    /// This limit is in characters, not bytes. See https://discuss.dev.twitch.tv/t/message-character-limit/7793/6
    private const int MaxMessageLength = 500;
    /// Maximum message length for whispers if the target user hasn't whispered us before.
    /// See also https://dev.twitch.tv/docs/api/reference/#send-whisper
    private const int MaxWhisperLength = 500;
    /// Maximum message length for whispers if the target user _has_ whispered us before.
    /// See also https://dev.twitch.tv/docs/api/reference/#send-whisper
    /// TODO Either Twitch's API is broken or their documentation is wrong,
    ///      because even for users that have whispered us before they just truncate the message to 500 characters.
    ///      See also https://discuss.dev.twitch.tv/t/whisper-truncated-to-500-characters-even-for-users-that-have-whispered-us-before/44844?u=felk
    // private const int MaxRepeatedWhisperLength = 10000;
    private const int MaxRepeatedWhisperLength = 500;

    private static readonly MessageSplitter MessageSplitterRegular = new(
        maxMessageLength: MaxMessageLength - "/me ".Length);

    private static readonly MessageSplitter MessageSplitterWhisperNeverWhispered = new(
        maxMessageLength: MaxWhisperLength);

    private static readonly MessageSplitter MessageSplitterWhisperWereWhisperedBefore = new(
        maxMessageLength: MaxRepeatedWhisperLength);

    private readonly ILogger<TwitchChat> _logger;
    private readonly string _channel;
    private readonly string _channelId;
    private readonly ImmutableHashSet<SuppressionType> _suppressions;
    private readonly ImmutableHashSet<string> _suppressionOverrides;
    private readonly TwitchChatQueue _queue;

    private readonly bool _useTwitchReplies;

    public TwitchChatSender(
        ILoggerFactory loggerFactory,
        TwitchApiProvider twitchApiProvider,
        ConnectionConfig.Twitch chatConfig,
        bool useTwitchReplies = true)
    {
        _logger = loggerFactory.CreateLogger<TwitchChat>();
        _channel = chatConfig.Channel;
        _channelId = chatConfig.ChannelId;
        _suppressions = chatConfig.Suppressions;
        _suppressionOverrides = chatConfig.SuppressionOverrides
            .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
        _useTwitchReplies = useTwitchReplies;

        _queue = new TwitchChatQueue(
            loggerFactory.CreateLogger<TwitchChatQueue>(),
            chatConfig.UserId,
            twitchApiProvider);
    }

    public async Task SendMessage(string message, Message? responseTo = null)
    {
        if (_suppressions.Contains(SuppressionType.Message) &&
            !_suppressionOverrides.Contains(_channel))
        {
            _logger.LogDebug("(suppressed) >#{Channel}: {Message}", _channel, message);
            return;
        }
        _logger.LogDebug(">#{Channel}: {Message}", _channel, message);
        await Task.Run(() =>
        {
            if (responseTo != null && !_useTwitchReplies)
                message = $"@{responseTo.User.Name} " + message;
            foreach (string part in MessageSplitterRegular.FitToMaxLength(message))
            {
                if (_useTwitchReplies && responseTo?.Details.MessageId != null)
                    _queue.Enqueue(responseTo.User,
                        new OutgoingMessage.Reply(_channelId, Message: "/me " + part,
                            ReplyToId: responseTo.Details.MessageId));
                else
                    _queue.Enqueue(responseTo?.User, new OutgoingMessage.Chat(_channelId, "/me " + part));
            }
        });
    }

    public async Task SendWhisper(User target, string message)
    {
        if (_suppressions.Contains(SuppressionType.Whisper) &&
            !_suppressionOverrides.Contains(target.SimpleName))
        {
            _logger.LogDebug("(suppressed) >@{Username}: {Message}", target.SimpleName, message);
            return;
        }
        _logger.LogDebug(">@{Username}: {Message}", target.SimpleName, message);
        bool newRecipient = target.LastWhisperReceivedAt == null;
        MessageSplitter splitter = newRecipient
            ? MessageSplitterWhisperNeverWhispered
            : MessageSplitterWhisperWereWhisperedBefore;
        await Task.Run(() =>
        {
            foreach (string part in splitter.FitToMaxLength(message))
            {
                _queue.Enqueue(target, new OutgoingMessage.Whisper(target.Id, part, newRecipient));
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _queue.DisposeAsync();
    }
}
