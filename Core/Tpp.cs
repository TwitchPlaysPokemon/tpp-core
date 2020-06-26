using System;
using System.Threading.Tasks;
using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core
{
    /// <summary>
    /// This class orchestrates the entirety of the TPP core.
    /// </summary>
    public class Tpp
    {
        private readonly ILogger _logger;
        private readonly RootConfig _rootConfig;

        public Tpp(ILoggerFactory loggerFactory, RootConfig rootConfig)
        {
            _logger = loggerFactory.CreateLogger<Tpp>();
            _rootConfig = rootConfig;
        }

        public async Task Run()
        {
            _logger.LogInformation("Hi!");
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            // TODO do all the things
            _logger.LogInformation("Bye!");
        }
    }
}
