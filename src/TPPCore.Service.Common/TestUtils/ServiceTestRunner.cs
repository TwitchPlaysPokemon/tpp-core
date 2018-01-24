using System.Threading.Tasks;
using CommandLine;

namespace TPPCore.Service.Common.TestUtils
{
    /// <summary>
    /// Runs the service runner in the background.
    /// </summary>
    /// <remarks>
    /// This class is intended for use in tests as the service is scheduled
    /// in the background. Call <see cref="SetUp"/> at the start of the test
    /// and <see cref="TearDown"/> at the end.
    /// </remarks>
    public class ServiceTestRunner
    {
        public readonly IService Service;
        public readonly ServiceRunner Runner;
        Task runAsyncTask;

        public ServiceTestRunner(IService service)
            : this(service, new ServiceRunner(service))
        {
        }

        public ServiceTestRunner(IService service, ServiceRunner runner)
        {
            this.Service = service;
            this.Runner = runner;
        }

        public void SetUp() {
            SetUpAsync().Wait();
        }

        public void SetUp(string[] args) {
            SetUpAsync(args).Wait();
        }

        public void SetUp(ServiceRunnerOptions options) {
            SetUpAsync(options).Wait();
        }

        public async Task SetUpAsync()
        {
            await SetUpAsync(new string[] {});
        }

        public async Task SetUpAsync(string[] args)
        {
            Runner.Configure(args);
            await setUpCommon();
        }

        public async Task SetUpAsync(ServiceRunnerOptions options)
        {
            Runner.Configure(options);
            await setUpCommon();
        }

        private async Task setUpCommon() {
            if (Runner.Context.RestfulServer.Context.LocalAuthenticationPassword == null)
            {
                Runner.Context.RestfulServer.SetPassword("testing");
                Runner.Context.InitRestfulClient();
            }

            await Runner.StartRestfulServerAsync();
            runAsyncTask = Runner.RunAsync();
        }

        public void TearDown()
        {
            TearDownAsync().Wait();
        }

        public async Task TearDownAsync()
        {
            Runner.Stop();
            await runAsyncTask;
            await Runner.StopRestfulServerAsync();
            Runner.CleanUp();
        }

        public static ServiceRunnerOptions GetDefaultOptions()
        {
            var argResult = Parser.Default.ParseArguments<ServiceRunnerOptions>(new string[] {});
            var parsedResult = (Parsed<ServiceRunnerOptions>) argResult;
            return parsedResult.Value;
        }
    }
}
