using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Chat.DataModels;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Chat.Tests
{
    public class DummyPubSubTest
    {
         private readonly ITestOutputHelper output;

        public DummyPubSubTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private ServiceTestRunner newServiceRunner()
        {
            return new ServiceTestRunner(new ChatService());
        }

        private ServiceRunnerOptions getOptions() {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(DummyRestfulTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Chat.Tests.test_files.dummy_config.yaml");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        [Fact]
        public async Task TestPubSubRead()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync(getOptions());
            await Task.Delay(500);
            await runner.TearDownAsync();

            var dummyPubSub = (DummyPubSubClient) runner.Runner.Context.PubSubClient;
            var message = dummyPubSub.Messages[0];

            Assert.Equal(ChatTopics.Message, message.Topic);

            var chatMessage = JsonConvert.DeserializeObject<ChatMessage>(message.Message);

            Assert.Equal("dummyTest", chatMessage.ClientName);
            Assert.Equal("dummy", chatMessage.ProviderName);
            Assert.StartsWith("someone", chatMessage.Sender.Username);
            Assert.StartsWith("hello", chatMessage.TextContent);
        }
    }
}
