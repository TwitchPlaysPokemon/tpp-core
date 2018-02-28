using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TPPCore.Service.Chat.DataModels;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Chat.Tests
{
    public class TwitchProviderTest
    {
        private readonly ITestOutputHelper output;

        public TwitchProviderTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private ServiceTestRunner newServiceRunner()
        {
            return new ServiceTestRunner(new ChatService());
        }

        private ServiceRunnerOptions getOptions()
        {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(TwitchProviderTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Chat.Tests.test_files.twitch_config.yaml");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        private void replaceConfigPort(ServiceRunnerOptions options, int port)
        {
            var newStream = new MemoryStream();
            var writer = new StreamWriter(newStream);

            using (StreamReader reader = new StreamReader(options.ConfigStream))
            {
                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null) {
                        break;
                    }

                    line = line.Replace("port: 0", "port: " + port);
                    writer.WriteLine(line);
                }
            }

            writer.Flush();
            newStream.Seek(0, SeekOrigin.Begin);
            options.ConfigStream = newStream;
        }

        [Fact]
        public async Task TestTwitchMock()
        {
            var mockServer = new MockTwitchIrcServer();
            var serverTask = mockServer.Run();
            var runner = newServiceRunner();
            var options = getOptions();

            replaceConfigPort(options, mockServer.Port);
            await runner.SetUpAsync(options);

            await Task.Delay(5000);

            var pubsub = (DummyPubSubClient) runner.Runner.Context.PubSubClient;

            output.WriteLine("Num messages {0}", pubsub.Messages.Count);
            Assert.NotEmpty(pubsub.Messages);

            var testFlag = false;
            // TODO: We want to check each type pub/sub topic is
            // working instead of just a single message.
            foreach (DummyPubSubClientMessage message in pubsub.Messages)
            {

                if (message.Topic == ChatTopics.Message)
                {
                    var chatMessage = JsonConvert.DeserializeObject<ChatMessage>(message.Message);
                    if (chatMessage.Channel == "#dallas" && chatMessage.TextContent == "cheer100")
                    {
                        testFlag = true;
                    }
                }
            }
            Assert.True(testFlag);

            await runner.TearDownAsync();
            mockServer.Stop();
            await serverTask;
        }
    }
}
