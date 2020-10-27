using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Match
{
    /// Match cycle for testing purposes that does not run an actual game.
    public class CoinflipMatchCycle : IMatchCycle
    {
        private static readonly Random Random = new Random();

        private static readonly IImmutableSet<Features.Capability> AllCapabilities =
            Enum.GetValues(typeof(Features.Capability)).Cast<Features.Capability>().ToImmutableHashSet();

        public Features.FeatureSet FeatureSet => new Features.FeatureSet(
            Generation: Features.Generation.Gen8,
            MaxTeamMembers: 3,
            Capabilities: AllCapabilities
        );

        private readonly ILogger<CoinflipMatchCycle> _logger;

        public CoinflipMatchCycle(ILogger<CoinflipMatchCycle> logger)
        {
            _logger = logger;
        }

        public async Task<IMatchCycle.Perform> SetUp(MatchInfo matchInfo, CancellationToken? token = null)
        {
            _logger.LogInformation("Setting up coinflip match...");
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: token ?? CancellationToken.None);
            return Perform;
        }

        private async Task<MatchResult> Perform(CancellationToken? token = null)
        {
            _logger.LogInformation("Performing coinflip match...");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken: token ?? CancellationToken.None);
            return new MatchResult(Winner: Random.Next(2) == 0 ? Side.Blue : Side.Red);
        }
    }
}
