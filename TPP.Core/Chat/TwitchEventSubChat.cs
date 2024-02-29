using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Utils;
using TPP.Twitch.EventSub;
using TPP.Twitch.EventSub.Notifications;
using static TPP.Core.EventUtils;

namespace TPP.Core.Chat;

public class TwitchEventSubChat : IWithLifecycle
{
    private readonly ILogger<TwitchEventSubChat> _logger;
    private readonly TwitchApiProvider _twitchApiProvider;

    private readonly EventSubClient _client;
    private readonly string _channelId;
    private readonly string _userId;

    public TwitchEventSubChat(
        ILoggerFactory loggerFactory,
        IClock clock,
        TwitchApiProvider twitchApiProvider,
        string channelId,
        string userId)
    {
        _logger = loggerFactory.CreateLogger<TwitchEventSubChat>();
        _twitchApiProvider = twitchApiProvider;
        _channelId = channelId;
        _userId = userId;

        _client = new EventSubClient(loggerFactory, clock);
        _client.RevocationReceived += (_, revocation) =>
            _logger.LogError("received revocation for {SubscriptionType}: {Data}",
                revocation.Metadata.SubscriptionType,
                JsonSerializer.Serialize(revocation.Payload.Subscription.Condition));
        _client.NotificationReceived += (_, notification) =>
        {
            if (notification is ChannelChatMessage chatMessage)
                _logger.LogInformation("received EventSub chat message, payload: {Payload}", chatMessage.Payload);
            else
                _logger.LogInformation("received other EventSub notification of type {Type}, payload:: {Payload}",
                    notification.Metadata.SubscriptionType, notification.Payload);
        };
        _client.Connected += (_, welcome) =>
            TaskToVoidSafely(_logger, () => SetUpSubscriptions(welcome.Payload.Session));
    }

    private async Task SetUpSubscriptions(Session session)
    {
        _logger.LogDebug("Setting up EventSub subscriptions");
        await session.SubscribeWithTwitchLibApi<ChannelChatMessage>(
            (await _twitchApiProvider.Get()).Helix.EventSub,
            new ChannelChatMessage.Condition(BroadcasterUserId: _channelId, UserId: _userId).AsDict());
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                _logger.LogDebug("EventSub websocket client connecting...");
                EventSubClient.DisconnectReason disconnectReason = await _client.ConnectAndReceive(cancellationToken);
                if (disconnectReason == EventSubClient.DisconnectReason.KeepaliveTimeout)
                    _logger.LogWarning("EventSub websocket closed because of keepalive timeout, reconnecting...");
                else if (disconnectReason == EventSubClient.DisconnectReason.RemoteDisconnected)
                    _logger.LogWarning("EventSub websocket unexpectedly closed by the remote, reconnecting...");
                else
                    _logger.LogWarning("EventSub websocket closed for unknown reasons ({Reason})", disconnectReason);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("EventSub websocket client was cancelled");
                break;
            }
        }
    }
}
