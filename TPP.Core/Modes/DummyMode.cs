using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Configuration;
using TPP.Core.Utils;

namespace TPP.Core.Modes
{
    /// A mode for testing purposes that can be run without any preconditions or configurations.
    public sealed class DummyMode : IWithLifecycle
    {
        private readonly ILogger<DummyMode> _logger;

        public DummyMode(ILoggerFactory loggerFactory, BaseConfig baseConfig)
        {
            _logger = loggerFactory.CreateLogger<DummyMode>();
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dummy mode starting");
            await cancellationToken.WhenCanceled();
            _logger.LogInformation("Dummy mode ended");
        }
    }
}
