using System.Threading.Tasks;
using TPPCore.Client.Example.Parrot;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Example.Parrot.Tests
{
    public class RestfulTest
    {
        private readonly ITestOutputHelper output;

        public RestfulTest(ITestOutputHelper output) {
            this.output = output;
        }

        private ServiceTestRunner NewServiceRunner()
        {
            return new ServiceTestRunner(new ParrotService());
        }

        private ServiceRunnerOptions GetOptions()
        {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(RestfulTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Example.Parrot.Tests.test_files.database_config.yaml");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        [Fact]
        public async Task TestCurrent()
        {
            var options = GetOptions();
            var runner = NewServiceRunner();
            await runner.SetUpAsync(options);

            var httpClient = runner.Runner.Context.RestfulClient;
            ParrotClient client = new ParrotClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), httpClient);
            var result = await client.GetCurrent();

            Assert.Equal("hello world!", result);

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestNewMessage()
        {
            var options = GetOptions();
            var runner = NewServiceRunner();
            await runner.SetUpAsync(options);

            var httpClient = runner.Runner.Context.RestfulClient;
            ParrotClient client = new ParrotClient(runner.Runner.Context.RestfulServer.Context.GetUri().ToString(), httpClient);
            await client.PostMessage("wow");

            await Task.Delay(1500);

            int Id = await client.GetMaxId();

            await Task.Delay(500);

            string contents = await client.GetContents(Id);

            Assert.Equal("wow", contents);

            await runner.TearDownAsync();
        }
    }
}
