using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TPP.Core.Overlay;

namespace TPP.Core.Tests.Overlay;

[Category("IntegrationTest"), Timeout(10_000)]
public class WebsocketBroadcastServerTest
{
    private int _port = 53427;

    private WebsocketBroadcastServer CreateServer() =>
        new(NullLogger<WebsocketBroadcastServer>.Instance, "localhost", _port);

    [SetUp]
    public void ChangePort()
    {
        _port++;
    }

    private async Task<WebsocketMessageStreamClient> CreateClient()
    {
        var url = new Uri($"ws://localhost:{_port}");
        var wsClient = new WebsocketMessageStreamClient();
        await wsClient.Connect(url, CancellationToken.None);
        return wsClient;
    }

    private static async Task AwaitConnectedClients(WebsocketBroadcastServer server, int n)
    {
        while (server.NumConnectedClients < n)
            await Task.Delay(TimeSpan.FromMilliseconds(1));
    }

    private static async Task<List<string>> ReadAllMessages(WebsocketMessageStreamClient client)
    {
        var messages = new List<string>();
        string? message;
        while ((message = await client.ReadAsync(CancellationToken.None)) != null)
        {
            messages.Add(message);
        }
        return messages;
    }

    [Test]
    public async Task read_messages_until_clean_shutdown()
    {
        await using WebsocketBroadcastServer server = CreateServer();
        Task _ = server.Listen();

        WebsocketMessageStreamClient client = await CreateClient();
        await AwaitConnectedClients(server, 1);
        Task<List<string>> readMessagesTask = ReadAllMessages(client);
        await server.Send("beep", CancellationToken.None);
        await server.Send("boop", CancellationToken.None);
        await server.Stop();
        Assert.That(await readMessagesTask, Is.EqualTo(new List<string> { "beep", "boop" }));
    }

    [Test]
    public async Task read_many_messages_from_many_clients()
    {
        await using WebsocketBroadcastServer server = CreateServer();
        Task _ = server.Listen();

        const int numMessages = 250;
        List<string> messages = Enumerable.Range(0, numMessages)
            .Select(i => $"message #{i}").ToList();

        const int numClients = 30;
        List<Task<WebsocketMessageStreamClient>> clientTasks = Enumerable
            .Range(0, numClients)
            .Select(_ => CreateClient())
            .ToList(); // start all coroutines to establish the connections concurrently
        await Task.WhenAll(clientTasks);
        await AwaitConnectedClients(server, numClients);
        List<Task<List<string>>> messageStreams = clientTasks
            .Select(task => ReadAllMessages(task.Result))
            .ToList(); // start all coroutines to start consuming websocket messages

        // ReSharper disable once AccessToDisposedClosure
        await Task.WhenAll(messages.Select(msg => server.Send(msg, CancellationToken.None)));
        await server.Stop();

        foreach (var stream in messageStreams)
        {
            Assert.That(await stream, Is.EqualTo(messages));
        }
    }

    [Test]
    public async Task client_cannot_send_messages()
    {
        await using WebsocketBroadcastServer server = CreateServer();
        Task _ = server.Listen();

        WebsocketMessageStreamClient client = await CreateClient();

        await client.WriteAsync("Hi server!", CancellationToken.None);
        Assert.IsNull(await client.ReadAsync(CancellationToken.None)); // server is terminating the connection
        WebSocketException ex = Assert.ThrowsAsync<WebSocketException>(()
            => client.WriteAsync("Websocket is already dead", CancellationToken.None))!;
        Assert.That(ex.WebSocketErrorCode, Is.EqualTo(WebSocketError.InvalidState));
    }

    [Test]
    public async Task send_after_client_disconnected()
    {
        await using WebsocketBroadcastServer server = CreateServer();
        Task _ = server.Listen();

        WebsocketMessageStreamClient client = await CreateClient();
        await AwaitConnectedClients(server, 1);
        await client.Disconnect(CancellationToken.None);

        await server.Send("plsnocrash", CancellationToken.None);
        await server.Stop();
        // exits cleanly
    }
}
