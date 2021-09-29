using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TPP.Core.Overlay;

public interface IBroadcastServer
{
    public Task Send(string message, CancellationToken cancellationToken);
}

internal readonly struct Connection : IAsyncDisposable
{
    internal IPEndPoint RemoteEndPoint { get; init; }
    internal WebSocket WebSocket { get; init; }
    internal Task ReaderTask { get; init; }
    internal CancellationTokenSource TokenSource { get; init; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await ReaderTask;
            WebSocket.Dispose();
        }
        catch (WebSocketException)
        {
            // we're just trying to get rid of it, even if the connection died unexpectedly
        }
    }
}

/// A websocket server capable of accepting new connections and broadcasting messages.
/// It cannot receive messages.
public class WebsocketBroadcastServer : IBroadcastServer, IAsyncDisposable
{
    private readonly List<Connection> _connections = new List<Connection>();
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<WebsocketBroadcastServer> _logger;
    private readonly SemaphoreSlim _connectionsSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

    private HttpListener? _httpListener;
    private CancellationTokenSource? _stopListenerToken;

    public WebsocketBroadcastServer(ILogger<WebsocketBroadcastServer> logger, string host, int port)
    {
        _logger = logger;
        if (host is "0.0.0.0" or "::")
        {
            logger.LogWarning(
                "Configured websocket host as '{Host}', but the host is being used as a http listener prefix, " +
                "not as a bind address. Assuming '*' instead to listen on all interfaces.", host);
            host = "*";
        }
        _host = host;
        _port = port;
    }

    public int NumConnectedClients => _connections.Count;

    /// Send a message to all currently connected clients.
    public async Task Send(string message, CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        List<WebSocketException> errors = new();
        await _connectionsSemaphore.WaitAsync(cancellationToken);
        try
        {
            await PruneDeadConnections();
            foreach (Connection connection in _connections)
            {
                try
                {
                    await connection.WebSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true,
                        cancellationToken);
                }
                catch (WebSocketException ex)
                {
                    if (ex.WebSocketErrorCode is WebSocketError.ConnectionClosedPrematurely or WebSocketError.InvalidState)
                    {
                        // the connection might unexpectedly die, but that's not our problem.
                        _logger.LogWarning(ex, "Could not send message to client");
                    }
                    else
                    {
                        errors.Add(ex);
                    }
                }
            }
        }
        finally
        {
            _connectionsSemaphore.Release();
        }
        if (errors.Any())
        {
            throw new AggregateException($"Sending failed for {errors.Count} connection(s)", errors);
        }
    }

    /// Handle incoming messages (which is just the close message) until the websocket closes.
    private async Task HandleUntilDead(WebSocket webSocket, IPEndPoint remoteEndPoint, CancellationToken token)
    {
        while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            WebSocketReceiveResult received = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(new byte[1024]), token);

            if (received.MessageType == WebSocketMessageType.Close)
            {
                if (webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                }
                _logger.LogInformation("Closed websocket connection from: {IP}", remoteEndPoint);
            }
            else
            {
                _logger.LogError("Unexpectedly received websocket data from {IP}, closing connection",
                    remoteEndPoint);
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation,
                    "clients must not send data themselves", token);
            }
        }
    }

    /// Remove and dispose all connections that are no longer alive.
    private async Task PruneDeadConnections()
    {
        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            Connection conn = _connections[i];
            if (conn.WebSocket.State == WebSocketState.Open) continue;
            _connections.RemoveAt(i);
            await conn.DisposeAsync();
        }
    }

    private async Task AddWebSocket(WebSocket ws, IPEndPoint remoteEndPoint)
    {
        await PruneDeadConnections();
        var tokenSource = new CancellationTokenSource();
        _connections.Add(new Connection
        {
            RemoteEndPoint = remoteEndPoint,
            WebSocket = ws,
            ReaderTask = HandleUntilDead(ws, remoteEndPoint, tokenSource.Token),
            TokenSource = tokenSource
        });
        _logger.LogInformation("New websocket connection from: {IP}", remoteEndPoint);
    }

    /// Using listener.GetContextAsync does not support passing a cancellation token
    /// and closes uncleanly when the listener is stopped, e.g. with an ObjectDisposedException.
    /// Using BeginGetContext and EndGetContext lets us abort in-between using a cancellation token.
    private static Task<HttpListenerContext> GetContextAsync(
        HttpListener listener, CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<HttpListenerContext>();
        void Accept(IAsyncResult result)
        {
            if (cancellationToken.IsCancellationRequested)
                taskCompletionSource.SetCanceled(cancellationToken);
            else
                taskCompletionSource.SetResult(listener.EndGetContext(result));
        }
        listener.BeginGetContext(Accept, null);
        return taskCompletionSource.Task;
    }

    /// Keeps accepting new incoming websocket connections until the server is stopped with <see cref="Stop"/>.
    public async Task Listen()
    {
        if (_httpListener != null)
            throw new InvalidOperationException("Cannot listen: The internal http listener is already running!");
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://{_host}:{_port}/");
        _httpListener.Start();
        _stopListenerToken = new CancellationTokenSource();

        while (_httpListener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await GetContextAsync(_httpListener, _stopListenerToken.Token);
            }
            catch (SystemException ex) when (ex is OperationCanceledException or HttpListenerException)
            {
                _logger.LogDebug("Websocket listener was stopped");
                break;
            }
            if (!context.Request.IsWebSocketRequest)
            {
                continue;
            }

            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null!);
            WebSocket webSocket = webSocketContext.WebSocket;
            await AddWebSocket(webSocket, context.Request.RemoteEndPoint);
        }
    }

    public async Task Stop(TimeSpan? closeHandshakeTimeout = null)
    {
        TimeSpan timeout = closeHandshakeTimeout ?? TimeSpan.FromSeconds(1);
        await _connectionsSemaphore.WaitAsync();
        try
        {
            await Task.WhenAll(_connections.Select(async connection =>
            {
                Task closeTask = connection.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                if (await Task.WhenAny(closeTask, Task.Delay(timeout)) != closeTask)
                {
                    _logger.LogWarning(
                        "websocket from {IP} takes longer than {Timeout}ms to finish the close handshake. Aborting the connection",
                        connection.RemoteEndPoint, timeout.TotalMilliseconds);
                    connection.WebSocket.Abort();
                }
                // Cancelling before finishing the close handshake puts the websocket in the "Aborted" state.
                // Cancelling _after_ finishing the close handshake is the websocket library's expected workflow
                // if the server initiates closing the websocket connection.
                connection.TokenSource.Cancel();
                try
                {
                    await connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected exception occured in websocket reader task");
                }
            }));
            _connections.Clear();
        }
        finally
        {
            _connectionsSemaphore.Release();
        }
        if (_httpListener is { IsListening: true })
        {
            _stopListenerToken?.Cancel();
            _httpListener.Stop();
        }
        _httpListener = null;
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
        await PruneDeadConnections();
        _connectionsSemaphore.Dispose();
    }
}
