using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPP.Core.Overlay;

/// A websocket client capable of connecting to a websocket server
/// and reading/writing messages using respective async methods.
public sealed class WebsocketMessageStreamClient : IAsyncDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private ClientWebSocket _ws = null!;

    private readonly byte[] _readBuffer = new byte[8192];

    public async Task Connect(Uri websocketUri, CancellationToken cancellationToken)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(websocketUri, cancellationToken);
    }

    public async Task Disconnect(CancellationToken cancellationToken)
    {
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws.State == WebSocketState.Open) await Disconnect(CancellationToken.None);
        _ws.Dispose();
    }

    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        var bufferSegment = new ArraySegment<byte>(_readBuffer);
        await using var ms = new MemoryStream();
        while (true)
        {
            WebSocketReceiveResult result = await _ws.ReceiveAsync(bufferSegment, cancellationToken);
            if (result.CloseStatus != null)
            {
                await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return null;
            }
            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new NotSupportedException();
            }
            await ms.WriteAsync(_readBuffer.AsMemory(0, result.Count), cancellationToken);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(ms, Utf8NoBom).ReadToEndAsync();
    }

    public async Task WriteAsync(string message, CancellationToken cancellationToken)
    {
        await _ws.SendAsync(Utf8NoBom.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);
    }
}
