using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common.Utils;
using TPP.Twitch.EventSub.Messages;
using static TPP.Twitch.EventSub.Parsing;

namespace TPP.Twitch.EventSub;

internal record WebsocketChangeover(ClientWebSocket NewWebSocket, SessionWelcome Welcome);

public class EventSubClient
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Duration KeepAliveGrace = Duration.FromSeconds(5);
    private static readonly Duration MaxMessageAge = Duration.FromMinutes(10);
    private static readonly Task<WebsocketChangeover> NoChangeoverTask =
        new TaskCompletionSource<WebsocketChangeover>().Task; // a task that never gets completed

    private readonly ILogger<EventSubClient> _logger;
    private readonly IClock _clock;
    private readonly Uri _uri;
    private int? _keepaliveTimeSeconds;
    private Duration KeepaliveDuration => Duration.FromSeconds(_keepaliveTimeSeconds ?? 600);

    private readonly TtlSet<string> _seenMessageIDs;

    public record DisconnectReason
    {
        private DisconnectReason() { } // simulate a sum type with a closed hierarchy
        public sealed record KeepaliveTimeout(Duration Timeout) : DisconnectReason;
        public sealed record ConnectionClosed : DisconnectReason;
        public sealed record WebsocketClosed(int CloseStatus, string? CloseStatusDescription) : DisconnectReason;
    }

    public event EventHandler<INotification>? NotificationReceived;
    public event EventHandler<Revocation>? RevocationReceived;
    public event EventHandler<SessionWelcome>? Connected;

    public EventSubClient(
        ILoggerFactory loggerFactory,
        IClock clock,
        string url = "wss://eventsub.wss.twitch.tv/ws",
        int? keepaliveTimeSeconds = null)
    {
        _logger = loggerFactory.CreateLogger<EventSubClient>();
        if (keepaliveTimeSeconds is < 10 or > 600)
            throw new ArgumentException(
                "Twitch only allows keepalive timeouts between 10 and 600 seconds", nameof(keepaliveTimeSeconds));

        _clock = clock;
        _uri = keepaliveTimeSeconds == null
            ? new Uri(url)
            : new Uri(url + "?keepalive_timeout_seconds=" + keepaliveTimeSeconds);
        _keepaliveTimeSeconds = keepaliveTimeSeconds;
        _seenMessageIDs = new TtlSet<string>(MaxMessageAge, _clock);
    }

    private record ReadMessageResponse
    {
        private ReadMessageResponse() { } // simulate a sum type with a closed hierarchy
        public sealed record Ok(ParseResult Result) : ReadMessageResponse;
        public sealed record ConnectionClosed : ReadMessageResponse;
        public sealed record WebsocketClosed(int CloseStatus, string? CloseStatusDescription) : ReadMessageResponse;
    }

    private static async Task<ReadMessageResponse> ReadMessage(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var bufferSegment = new ArraySegment<byte>(new byte[8192]);
        await using var ms = new MemoryStream();
        while (!cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await webSocket.ReceiveAsync(bufferSegment, cancellationToken);
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // This isn't a nice way to close the connection, but let's treat it as a normal closure for simplicity.
                return new ReadMessageResponse.ConnectionClosed();
            }
            if (result.CloseStatus != null)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return new ReadMessageResponse.WebsocketClosed((int)result.CloseStatus.Value,
                    result.CloseStatusDescription);
            }
            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new NotSupportedException();
            }
            await ms.WriteAsync(bufferSegment.AsMemory(0, result.Count), cancellationToken);
            if (result.EndOfMessage)
            {
                break;
            }
        }
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();

        ms.Seek(0, SeekOrigin.Begin);
        string json = await new StreamReader(ms, Utf8NoBom).ReadToEndAsync(cancellationToken);
        return new ReadMessageResponse.Ok(Parse(json));
    }

    /// <summary>
    /// You cannot usually swap variables used in a using declaration.
    /// But by deferring the disposal to a mutable inner instance, we can achieve declaring this with "using",
    /// but swapping its content midway.
    /// </summary>
    private class WebSocketBox : IDisposable
    {
        public ClientWebSocket WebSocket { get; private set; } = new();
        public ClientWebSocket SwapWith(ClientWebSocket newWebSocket)
        {
            ClientWebSocket oldWebSocket = WebSocket;
            WebSocket = newWebSocket;
            return oldWebSocket;
        }
        public void Dispose() => WebSocket.Dispose();
    }

    public async Task<DisconnectReason> ConnectAndReceive(CancellationToken cancellationToken)
    {
        bool welcomeReceived = false;
        Task<WebsocketChangeover> changeoverTask = NoChangeoverTask;

        using var webSocketBox = new WebSocketBox();
        if (_keepaliveTimeSeconds != null)
            webSocketBox.WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(_keepaliveTimeSeconds.Value);
        await webSocketBox.WebSocket.ConnectAsync(_uri, cancellationToken);
        Instant lastMessageTimestamp = _clock.GetCurrentInstant(); // treat a fresh connection as a received message
        while (!cancellationToken.IsCancellationRequested)
        {
            // Don't pass the cancellation token here. Instead, once cancelled we perform a graceful websocket closure.
            // Otherwise the websocket would be immediately put in an aborted state, but we're not in that of a hurry.
            Task<ReadMessageResponse> readTask = ReadMessage(webSocketBox.WebSocket, CancellationToken.None);
            Instant assumeDeadAt = lastMessageTimestamp + KeepaliveDuration + KeepAliveGrace;
            Instant now = _clock.GetCurrentInstant();
            Task timeoutTask = assumeDeadAt < now
                ? Task.CompletedTask
                : Task.Delay((assumeDeadAt - now).ToTimeSpan(), cancellationToken);
            Task firstFinishedTask = await Task.WhenAny(changeoverTask, readTask, timeoutTask);
            if (cancellationToken.IsCancellationRequested)
                break;
            if (firstFinishedTask == changeoverTask)
            {
                WebsocketChangeover changeover = await changeoverTask;
                _logger.LogDebug("Finished WebSocket Changeover, welcome message: {Welcome}", changeover.Welcome);
                _keepaliveTimeSeconds = changeover.Welcome.Payload.Session.KeepaliveTimeoutSeconds;
                lastMessageTimestamp = changeover.Welcome.Metadata.MessageTimestamp;
                changeoverTask = NoChangeoverTask;
                ClientWebSocket oldWebsocket = webSocketBox.SwapWith(changeover.NewWebSocket);
                await oldWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                oldWebsocket.Dispose();
                continue;
            }
            if (firstFinishedTask == timeoutTask)
                // Regarding "Keepalive message", Twitch recommends:
                // If your client doesn't receive an event or keepalive message for longer than keepalive_timeout_seconds,
                // you should assume the connection is lost and reconnect to the server and resubscribe to the events.
                return new DisconnectReason.KeepaliveTimeout(KeepaliveDuration);
            ReadMessageResponse messageResult = await readTask;
            if (messageResult is ReadMessageResponse.ConnectionClosed)
                return new DisconnectReason.ConnectionClosed();
            if (messageResult is ReadMessageResponse.WebsocketClosed closed)
                return new DisconnectReason.WebsocketClosed(closed.CloseStatus, closed.CloseStatusDescription);
            if (messageResult is not ReadMessageResponse.Ok(var parseResult))
                throw new ArgumentOutOfRangeException(nameof(messageResult));

            if (parseResult is not ParseResult.Ok(var message))
            {
                if (parseResult is ParseResult.InvalidMessage(var error))
                    _logger.LogError("A message was skipped because it failed to parse: {Error}", error);
                else if (parseResult is ParseResult.UnknownMessageType(var messageType))
                    _logger.LogWarning("Unknown message type received and skipped: {MessageType}", messageType);
                else if (parseResult is ParseResult.UnknownSubscriptionType(var subType))
                    _logger.LogWarning("Unknown subscription type received and skipped: {SubscriptionType}", subType);
                else
                    throw new ArgumentOutOfRangeException(nameof(parseResult));
                continue;
            }
            if (message.Metadata.MessageTimestamp < _clock.GetCurrentInstant() - MaxMessageAge)
            {
                // Regarding "Guarding against replay attacks", Twitch recommends:
                // Make sure the value in the message_timestamp field isn’t older than 10 minutes.
                throw new ProtocolViolationException(
                    $"Unexpectedly received message older than {MaxMessageAge.TotalSeconds}s: {message}");
            }
            if (!_seenMessageIDs.Add(message.Metadata.MessageId))
            {
                // Regarding "Guarding against replay attacks", Twitch recommends:
                // Make sure you haven’t seen the ID in the message_id field before.
                _logger.LogWarning("Dropping duplicate message with ID {Id}, Message: {Message}",
                    message.Metadata.MessageId, message);
                continue; // Just drop.
            }
            Instant clientNow = _clock.GetCurrentInstant();
            Duration clientServerTimeShift = clientNow - message.Metadata.MessageTimestamp;
            if (clientServerTimeShift < -KeepAliveGrace || clientServerTimeShift > KeepAliveGrace)
            {
                _logger.LogWarning(
                    "Client time is {Shift}s ahead of server time: Received message with timestamp {ServerTimestamp}, " +
                    "but client timestamp currently is {ClientTimestamp}. " +
                    "You may want to either increase 'KeepAliveGrace' or fix the clock accuracy.",
                    clientServerTimeShift.TotalSeconds, message.Metadata.MessageTimestamp, clientNow);
            }
            lastMessageTimestamp = message.Metadata.MessageTimestamp;
            if (message is SessionWelcome welcome)
            {
                if (welcomeReceived)
                    throw new ProtocolViolationException(
                        $"Unexpected received a second welcome message on websocket: {message}");
                welcomeReceived = true;
                _keepaliveTimeSeconds = welcome.Payload.Session.KeepaliveTimeoutSeconds;
                Connected?.Invoke(this, welcome);
            }
            else if (!welcomeReceived)
            {
                throw new ProtocolViolationException(
                    $"Expected first message on websocket to be {SessionWelcome.MessageType}, " +
                    $"but unexpectedly was {message.Metadata.MessageType}");
            }
            else if (message is INotification notification)
            {
                NotificationReceived?.Invoke(this, notification);
            }
            else if (message is SessionReconnect reconnect)
            {
                var reconnectUri = new Uri(reconnect.Payload.Session
                    .ReconnectUrl ?? throw new ProtocolViolationException(
                    "twitch must provide a reconnect URL in a reconnect message"));
                _logger.LogDebug("Initiating WebSocket Changeover to new Uri: {ReconnectUri}", reconnectUri);
                changeoverTask = PerformChangeover(reconnectUri, cancellationToken);
            }
            else if (message is Revocation revocation)
            {
                RevocationReceived?.Invoke(this, revocation);
            }
            else if (message is SessionKeepalive)
            {
                // Do nothing. All this is good for is resetting the timestamp of the last received message
                // to honor the keepalive time, which gets done for all messages above.
            }
            else
            {
                _logger.LogError("Known message was not handled and skipped: {Message}", message);
            }
        }
        if (webSocketBox.WebSocket.State == WebSocketState.Open)
            await webSocketBox.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        else
            webSocketBox.WebSocket.Abort();
        throw new TaskCanceledException();
    }

    private static async Task<WebsocketChangeover> PerformChangeover(Uri reconnectUri,
        CancellationToken cancellationToken)
    {
        var newWebSocket = new ClientWebSocket();
        await newWebSocket.ConnectAsync(reconnectUri, cancellationToken);
        ReadMessageResponse firstReceivedWebsocketMessage = await ReadMessage(newWebSocket, cancellationToken);
        if (firstReceivedWebsocketMessage is not ReadMessageResponse.Ok(var firstReceivedMessage))
            throw new IOException("Unexpectedly lost connection on new connection after changeover");
        if (firstReceivedMessage is ParseResult.Ok(SessionWelcome welcomeOnNewWebsocket))
        {
            // Small delay so it's unlikely we drop already in-flight messages from the old websocket.
            // Tradeoff is that we delay new messages for up to this amount after a reconnect.
            //await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return new WebsocketChangeover(newWebSocket, welcomeOnNewWebsocket);
        }
        else if (firstReceivedMessage is ParseResult.Ok(var anyOtherMessage))
        {
            throw new ProtocolViolationException(
                $"Expected first message on reconnect websocket to be {SessionWelcome.MessageType}, " +
                $"but unexpectedly was {anyOtherMessage.Metadata.MessageType}");
        }
        else
        {
            throw new ProtocolViolationException(
                $"Expected first message on reconnect websocket to be {SessionWelcome.MessageType}, " +
                $"but couldn't understand the message: {firstReceivedMessage}");
        }
    }
}
