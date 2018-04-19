using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TPPCore.Service.Chat;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.ChatLogger.Tests
{
    public class LogTest
    {
        private readonly ITestOutputHelper output;

        private Dictionary<string, string> Meta = new Dictionary<string, string>
        {
            {"test1", "test2" }
        };

        private ServiceRunnerOptions getOptions()
        {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(LogTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.ChatLogger.Tests.test_files.chat_log_test_config.yaml");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        public LogTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private ServiceTestRunner NewServiceRunner()
        {
            return new ServiceTestRunner(new ChatLoggerService());
        }

        private JObject ToJObject(string RawContent)
        {
            return JObject.FromObject(new
            {
                topic = ChatTopics.Raw,
                clientName = "test",
                providerName = "test",
                isSelf = false,
                meta = Meta,
                rawContent = RawContent
            });
        }

        [Fact]
        public async Task TestLogReadWrite()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(getOptions());
            await Task.Delay(500);

            if (File.Exists("logs/test" + DateTime.UtcNow.Date.ToString("yyyy'-'MM'-'dd") + ".log"))
                File.Delete("logs/test" + DateTime.UtcNow.Date.ToString("yyyy'-'MM'-'dd") + ".log");

            var dummyPubSub = (DummyPubSubClient)runner.Runner.Context.PubSubClient;

            var jsonMessage = ToJObject(@"@badges=staff/1,bits/1000;bits=100;color=;display-name=dallas;emotes=;id=b34ccfc7-4977-403a-8a94-33c6bac34fb8;mod=0;room-id=1337;subscriber=0;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=staff :ronni!ronni@ronni.tmi.twitch.tv PRIVMSG #dallas :cheer100");
            dummyPubSub.Publish(ChatTopics.Raw, jsonMessage);
            string written1 = LogManager.dateTime.ToString("o") + " " + @"@badges=staff/1,bits/1000;bits=100;color=;display-name=dallas;emotes=;id=b34ccfc7-4977-403a-8a94-33c6bac34fb8;mod=0;room-id=1337;subscriber=0;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=staff :ronni!ronni@ronni.tmi.twitch.tv PRIVMSG #dallas :cheer100";

            await Task.Delay(500);

            dummyPubSub.Publish(ChatTopics.Raw, jsonMessage);
            string written2 = LogManager.dateTime.ToString("o") + " " + @"@badges=staff/1,bits/1000;bits=100;color=;display-name=dallas;emotes=;id=b34ccfc7-4977-403a-8a94-33c6bac34fb8;mod=0;room-id=1337;subscriber=0;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=staff :ronni!ronni@ronni.tmi.twitch.tv PRIVMSG #dallas :cheer100";

            await Task.Delay(500);

            List<string> lines = (File.ReadLines(LogManager.FilePath)).ToList();
            Assert.Equal(written1, lines[0]);
            Assert.Equal(written2, lines[1]);
            await runner.TearDownAsync();
        }
    }
}
