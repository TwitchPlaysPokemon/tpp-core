using System.Threading.Tasks;
using TPPCore.Client.Example.Parrot;
using TPPCore.Service.Common.TestUtils;
using Xunit;

namespace TPPCore.Service.Example.Parrot.Tests
{
    public class RestfulTest
    {
        public RestfulTest() {
        }

        private ServiceTestRunner NewServiceRunner()
        {
            return new ServiceTestRunner(new ParrotService());
        }

        [Fact]
        public async Task TestCurrent()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync();

            var httpClient = runner.Runner.Context.RestfulClient;
            ParrotClient client = new ParrotClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), httpClient);
            var result = await client.GetCurrent();

            Assert.Equal("hello world!", result);

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestNewMessage()
        {
            var runner = NewServiceRunner();
            await runner.SetUpAsync();

            var httpClient = runner.Runner.Context.RestfulClient;
            ParrotClient client = new ParrotClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), httpClient);
            await client.PostMessage("wow");

            await runner.TearDownAsync();
        }
    }
}
