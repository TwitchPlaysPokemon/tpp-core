using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Client.Chat;
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

        private ServiceTestRunner NewServiceRunner()
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
        public async Task TestUserIdUsername()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(getOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", httpClient);
            var result = await client.GetUserName();
            Assert.Equal("dummy", result);

            result = await client.GetUserId();
            Assert.Equal("dummy", result);

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestRoomList()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(getOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", httpClient);
            var result = await client.GetRoomList("%23somechannel");
            ChatUser dummy = result.Viewers[0];
            Assert.Equal(2, result.NumUsers);
            Assert.Equal("dummy", dummy.Username);

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestSendMessage()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(getOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", httpClient);
            await client.SendMessage("%23somechannel", "hello world");

            await runner.TearDownAsync();
        }
    }
}
