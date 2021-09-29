using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InputMap = System.Collections.Generic.IDictionary<string, object>;

namespace TPP.Core;

public sealed class InputServer : IDisposable
{
    private readonly ILogger<InputServer> _logger;
    private readonly string _host;
    private readonly int _port;

    private HttpListener? _httpListener;
    public IInputFeed InputFeed;

    public InputServer(
        ILogger<InputServer> logger,
        string host, int port,
        IInputFeed inputFeed)
    {
        _logger = logger;
        if (host is "0.0.0.0" or "::")
        {
            logger.LogWarning(
                "Configured input server host as '{Host}', but the host is being used as a http listener prefix, " +
                "not as a bind address. Assuming '*' instead to listen on all interfaces.", host);
            host = "*";
        }
        _host = host;
        _port = port;
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
                try
                {
                    InputMap? inputMap = await InputFeed.HandleRequest(request.RawUrl?.ToLower());
                    responseText = inputMap == null ? null : JsonSerializer.Serialize(inputMap);
                }
                catch (ArgumentException)
                {
                    response.StatusCode = 400;
                    response.Close();
                    continue;
                }

                Stream output = response.OutputStream;
                if (responseText != null)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                    response.ContentLength64 = buffer.Length;
                    await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
                }
                output.Close();
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
