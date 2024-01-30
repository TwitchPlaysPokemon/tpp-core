using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Inputting;
using InputMap = System.Collections.Generic.IDictionary<string, object>;

namespace TPP.Core
{
    public sealed class InputServer : IDisposable
    {
        private readonly ILogger<InputServer> _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly MuteInputsToken _muteInputsToken;
        private readonly Func<IInputFeed> _inputFeedSupplier;

        private bool _stopped = false;
        private HttpListener? _httpListener;

        public InputServer(
            ILogger<InputServer> logger,
            string host, int port,
            MuteInputsToken muteInputsToken,
            Func<IInputFeed> inputFeedSupplier)
        {
            _logger = logger;
            if (host is "0.0.0.0" or "::")
            {
                logger.LogWarning(
                    "Configured input server host as '{Host}', but the host is being used as a http listener prefix, " +
                    "not as a bind address. Assuming '*' instead to listen on all interfaces", host);
                host = "*";
            }
            if (host is "localhost")
                logger.LogWarning("Configured input server host as '{Host}' instead of '127.0.0.1. " +
                                  "It might not be reachable from 127.0.0.1", host);
            _host = host;
            _port = port;
            _muteInputsToken = muteInputsToken;
            _inputFeedSupplier = inputFeedSupplier;
        }

        /// <summary>
        /// Keeps responding to new incoming requests until the server is stopped with <see cref="Stop"/>.
        /// </summary>
        public async Task Listen()
        {
            if (_httpListener != null)
                throw new InvalidOperationException("Cannot listen: The internal http listener is already running!");
            _httpListener = new HttpListener();

            // Stop any lingering connections from filling the request queue
            _httpListener.TimeoutManager.IdleConnection = TimeSpan.FromSeconds(1);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _httpListener.TimeoutManager.RequestQueue = TimeSpan.FromMilliseconds(100);
                _httpListener.TimeoutManager.HeaderWait = TimeSpan.FromMilliseconds(100);
                _httpListener.TimeoutManager.EntityBody = TimeSpan.FromMilliseconds(100);
            }

            _httpListener.Prefixes.Add($"http://{_host}:{_port}/");
            _httpListener.Start();
            _logger.LogInformation("Started input server on {Prefixes}", _httpListener.Prefixes);

            // put responding to input requests on its own thread to avoid any potential
            // delays due to cooperative multitasking
            await Task.Run(async () =>
            {

                while (_httpListener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _httpListener.GetContextAsync();
                    }
                    catch (HttpListenerException)
                    {
                        _logger.LogDebug("input server listener was stopped");
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        // GetContextAsync doesn't take a cancellation token,
                        // and stopping the http server can cause it to trip over itself for some reason.
                        _logger.LogDebug("Encountered ObjectDisposedException while accepting an incoming connection");
                        return;
                    }

                    try
                    {
                        const long timeoutMs = 1000;
                        Task handleConnection = HandleSingleConnection(context.Request, context.Response);
                        Task timeout = Task.Delay(TimeSpan.FromMilliseconds(timeoutMs));
                        if (await Task.WhenAny(handleConnection, timeout) == timeout)
                            throw new InvalidOperationException($"Request took too long, timeout was {timeoutMs}ms");
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "Encountered an error handling an incoming request, dropping connection");
                        context.Response.Close();
                    }
                }
            });

            if (!_stopped)
                throw new InvalidOperationException(
                    "Unexpectedly encountered a graceful shutdown (listening ended but no stop was requested)");
        }

        private async Task HandleSingleConnection(HttpListenerRequest request, HttpListenerResponse response)
        {
            string? responseText;
            string? requestUrl = request.RawUrl?.ToLower();
            try
            {
                if (requestUrl == "/start_run")
                {
                    _muteInputsToken.Muted = false;
                    responseText = "ok";
                }
                else if (requestUrl == "/stop_run")
                {
                    _muteInputsToken.Muted = true;
                    responseText = "ok";
                }
                else
                {
                    InputMap? inputMap = await _inputFeedSupplier().HandleRequest(requestUrl);
                    responseText = inputMap == null ? null : JsonSerializer.Serialize(inputMap);
                }
            }
            catch (ArgumentException ex)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                try
                {
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
                    response.StatusCode = 400;
                    response.Close();
                }
                catch (HttpListenerException httpEx)
                {
                    _logger.LogError(httpEx,
                        "Failed to send input listener exception as response: {Exception}", ex.ToString());
                }
                return;
            }

            if (responseText != null)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }
            response.Close();
        }

        public void Stop()
        {
            _stopped = true;
            if (_httpListener is { IsListening: true })
            {
                _httpListener.Stop();
            }
            _httpListener = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
