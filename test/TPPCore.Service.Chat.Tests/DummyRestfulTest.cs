using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Chat.Tests
{
    public class DummyRestfulTest
    {
         private readonly ITestOutputHelper output;

        public DummyRestfulTest(ITestOutputHelper output)
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
        public async Task TestRoomList()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync(getOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            var uri = new Uri(
                runner.Runner.Context.RestfulServer.Context.GetUri(),
                "chat/dummy/%23somechannel/room_list");
            var result = await httpClient.GetJsonAsync(uri);

            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var jsonDoc = result.JsonDoc;
            var users = jsonDoc.Value<JArray>("users");

            Assert.Equal(2, users.Count);
            Assert.Equal("dummy", (string) users.SelectToken("[0].username"));

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestSendMessage()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync(getOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            var uri = new Uri(
                runner.Runner.Context.RestfulServer.Context.GetUri(),
                "chat/dummy/%23somechannel/send");
            var inputDoc = JObject.FromObject(new { message = "hello world" });
            var result = await httpClient.PostJsonAsync(uri, inputDoc);

            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            await runner.TearDownAsync();
        }
    }
}
