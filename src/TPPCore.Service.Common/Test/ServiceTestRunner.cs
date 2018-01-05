using System.Threading.Tasks;

namespace TPPCore.Service.Common.Test
{
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

        public async Task SetUpAsync()
        {
            await SetUpAsync(new string[] {});
        }

        public async Task SetUpAsync(string[] args)
        {
            Runner.Configure(args);
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
    }
}
