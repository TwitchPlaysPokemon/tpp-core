using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.Overlay
{
    public interface IBroadcastServer
    {
        public Task Send(string message, CancellationToken cancellationToken);
    }

    /// A websocket server capable of accepting new connections and broadcasting messages.
    /// It cannot receive messages.
    public class WebsocketBroadcastServer : IBroadcastServer, IAsyncDisposable
    {
        private readonly List<Connection> _connections = new List<Connection>();
        private HttpListener? _httpListener;

        private struct Connection : IAsyncDisposable
        {
            internal IPEndPoint RemoteEndPoint { get; init; }
            internal WebSocket WebSocket { get; init; }
            internal Task ReaderTask { get; init; }
            internal CancellationTokenSource TokenSource { get; init; }

            public async ValueTask DisposeAsync()
            {
                await ReaderTask;
                WebSocket.Dispose();
            }
        }

        private readonly string _host;
        private readonly int _port;
        private readonly ILogger<WebsocketBroadcastServer> _logger;
        private readonly SemaphoreSlim _connectionsSemaphore = new SemaphoreSlim(initialCount: 100, maxCount: 100);

        public WebsocketBroadcastServer(ILogger<WebsocketBroadcastServer> logger, string host, int port)
        {
            _logger = logger;
            _host = host;
            _port = port;
        }

        /// Send a message to all currently connected clients.
        public async Task Send(string message, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await _connectionsSemaphore.WaitAsync(cancellationToken);
            try
            {
                foreach (var connection in _connections)
                {
                    await connection.WebSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true,
                        cancellationToken);
                }
            }
            finally
            {
                _connectionsSemaphore.Release();
            }
        }

        /// Handle incoming messages (which is just the close message) until the websocket closes.
        private async Task HandleUntilDead(WebSocket webSocket, CancellationToken token)
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
                    _logger.LogInformation($"Closed websocket connection: {webSocket}");
                }
                else
                {
                    _logger.LogError($"Unexpectedly received data from websocket {webSocket}, closing connection");
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
                ReaderTask = HandleUntilDead(ws, tokenSource.Token),
                TokenSource = tokenSource
            });
            _logger.LogInformation($"New websocket connection: {ws}");
        }

        /// Keeps accepting new incoming websocket connections until the server is stopped with <see cref="Stop"/>.
        public async Task Listen()
        {
            if (_httpListener != null)
                throw new InvalidOperationException("Cannot listen: The internal http listener is already running!");
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://{_host}:{_port}/");
            _httpListener.Start();

            while (_httpListener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _httpListener.GetContextAsync();
                }
                catch (HttpListenerException)
                {
                    _logger.LogInformation("Websocket listener was stopped.");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // GetContextAsync doesn't take a cancellation token,
                    // and stopping the http server can cause it to trip over itself for some reason.
                    _logger.LogError("Encountered ObjectDisposedException while accepting a websocket connection.");
                    return;
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
                            $"websocket from {connection.RemoteEndPoint} takes longer than {timeout.TotalMilliseconds}ms " +
                            "to finish the close handshake. Aborting the connection.");
                        connection.WebSocket.Abort();
                    }
                    // Cancelling before finishing the close handshare puts the websocket in the "Aborted" state.
                    // Cancelling _after_ finishing the close handshare is the websocket library's expected workflow
                    // if the server initiates closing the websocket connection.
                    connection.TokenSource.Cancel();
                    try
                    {
                        await connection.ReaderTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception occured in websocket reader task");
                    }
                }));
                _connections.Clear();
            }
            finally
            {
                _connectionsSemaphore.Release();
            }
            if (_httpListener != null && _httpListener.IsListening)
            {
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
}
