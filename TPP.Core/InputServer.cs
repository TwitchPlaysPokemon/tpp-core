using System;
using System.Net;
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

        private HttpListener? _httpListener;
        public IInputFeed InputFeed;

        public InputServer(
            ILogger<InputServer> logger,
            string host, int port,
            MuteInputsToken muteInputsToken,
            IInputFeed inputFeed)
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
            InputFeed = inputFeed;
        }

        /// <summary>
        /// Keeps responding to new incoming requests until the server is stopped with <see cref="Stop"/>.
        /// </summary>
        public async Task Listen()
        {
            if (_httpListener != null)
                throw new InvalidOperationException("Cannot listen: The internal http listener is already running!");
            _httpListener = new HttpListener();
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
                        _logger.LogError("Encountered ObjectDisposedException while accepting an incoming connection");
                        return;
                    }

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

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
                            InputMap? inputMap = await InputFeed.HandleRequest(requestUrl);
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
                        continue;
                    }

                    if (responseText != null)
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
                    }
                    response.Close();
                }
            });
        }

        public void Stop()
        {
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
