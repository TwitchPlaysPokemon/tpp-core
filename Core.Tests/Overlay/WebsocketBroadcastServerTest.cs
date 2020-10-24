using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Core.Overlay;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Core.Tests.Overlay
{
    [Category("IntegrationTest")]
    public class WebsocketBroadcastServerTest
    {
        private const int Port = 12345;
        private Task _wsServerListen = null!;
        private WebsocketBroadcastServer _server = null!;

        [SetUp]
        public void SetUp()
        {
            _server = new WebsocketBroadcastServer(NullLogger<WebsocketBroadcastServer>.Instance, "localhost", Port);
            _wsServerListen = _server.Listen();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _server.Stop();
            await _wsServerListen;
        }

        private static async Task<WebsocketMessageStreamClient> CreateConnectedClient()
        {
            var url = new Uri($"ws://localhost:{Port}");
            var wsClient = new WebsocketMessageStreamClient();
            await wsClient.Connect(url, CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(10)); // wait for the server to accept the connection
            return wsClient;
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
            WebsocketMessageStreamClient client = await CreateConnectedClient();
            Task<List<string>> readMessagesTask = ReadAllMessages(client);
            await _server.Send("beep", CancellationToken.None);
            await _server.Send("boop", CancellationToken.None);
            await _server.Stop();
            Assert.AreEqual(new List<string> { "beep", "boop" }, await readMessagesTask);
        }

        [Test]
        public async Task read_many_messages_from_many_clients()
        {
            const int numMessages = 250;
            List<string> messages = Enumerable.Range(0, numMessages)
                .Select(i => $"message #{i}").ToList();

            const int numClients = 100;
            List<Task<WebsocketMessageStreamClient>> clientTasks = Enumerable
                .Range(0, numClients)
                .Select(_ => CreateConnectedClient())
                .ToList(); // start all coroutines to establish the connections concurrently
            List<Task<List<string>>> messageStreams = clientTasks
                .Select(task => ReadAllMessages(task.Result))
                .ToList(); // start all coroutines to start consuming websocket messages

            await Task.WhenAll(messages.Select(msg => _server.Send(msg, CancellationToken.None)));
            await _server.Stop();

            foreach (var stream in messageStreams)
            {
                Assert.AreEqual(messages, await stream);
            }
        }

        [Test]
        public async Task client_cannot_send_messages()
        {
            WebsocketMessageStreamClient client = await CreateConnectedClient();

            await client.WriteAsync("Hi server!", CancellationToken.None);
            Assert.IsNull(await client.ReadAsync(CancellationToken.None)); // server is terminating the connection
            var ex = Assert.ThrowsAsync<WebSocketException>(()
                => client.WriteAsync("Websocket is already dead", CancellationToken.None));
            Assert.AreEqual(WebSocketError.InvalidState, ex.WebSocketErrorCode);
        }
    }
}
