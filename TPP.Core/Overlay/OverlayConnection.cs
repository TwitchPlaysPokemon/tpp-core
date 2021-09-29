using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace TPP.Core.Overlay;

/// Adapter over a broadcast server (usually via websocket) that is able to send messages to the (legacy) overlay.
/// It takes instances of <see cref="IOverlayEvent"/> and sends them as JSON message in the appropriate dialect.
public class OverlayConnection
{
    private readonly IBroadcastServer _broadcastServer;
    private readonly Func<object, string> _serializer;
    private readonly ILogger<OverlayConnection> _logger;

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Converters = new List<JsonConverter> { new StringEnumConverter() }
    };

    public OverlayConnection(ILogger<OverlayConnection> logger, IBroadcastServer broadcastServer)
    {
        _logger = logger;
        _broadcastServer = broadcastServer;
        _serializer = obj => JsonConvert.SerializeObject(obj, SerializerSettings);
    }

    public async Task Send(IOverlayEvent evt, CancellationToken cancellationToken)
    {
        string text = _serializer(new
        {
            type = evt.OverlayEventType,
            extra_parameters = evt
        });
        _logger.LogDebug("sending overlay event of type '{EventType}', text: {EventText}",
            evt.OverlayEventType, text);
        await _broadcastServer.Send(text, cancellationToken);
    }
}
