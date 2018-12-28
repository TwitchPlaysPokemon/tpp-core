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

        private ServiceRunnerOptions GetOptions() {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(DummyRestfulTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Chat.Tests.test_files.dummy_config.json");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        [Fact]
        public async Task TestUserIdUsername()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", "#somechannel", httpClient);
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
            await runner.SetUpAsync(GetOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", "#somechannel", httpClient);
            var result = await client.GetRoomList();
            ChatUser dummy = result[0];
            Assert.Equal(2, result.Count);
            Assert.Equal("dummy", dummy.Username);

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestSendMessage()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", "#somechannel", httpClient);
            await client.SendMessage("hello world");

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestTimeoutBan()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            var httpClient = runner.Runner.Context.RestfulClient;
            ChatClient client = new ChatClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), "dummyTest", "#somechannel", httpClient);
            await client.TimeoutUser(new ChatUser() { UserId = "dummy", Username = "dummy", Nickname = "Dummy", AccessLevel = AccessLevel.Viewer }, "copypasta", 200);

            await Task.Delay(200);
            await client.BanUser(new ChatUser() { UserId = "dummy", Username = "dummy", Nickname = "Dummy", AccessLevel = AccessLevel.Viewer }, "copypasta");

            await runner.TearDownAsync();
        }
    }
}
