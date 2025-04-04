using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Model;

namespace TPP.Match;

/// Match cycle for testing purposes that does not run an actual game.
public class CoinflipMatchCycle(ILogger<CoinflipMatchCycle> logger) : IMatchCycle
{
    public GameId GameId => GameId.Coinflip;

    private static readonly Random Random = new();

    public Features.FeatureSet FeatureSet => new(
        Generation: Features.Generation.Gen8,
        MaxTeamMembers: 3,
        Capabilities: Enum.GetValues<Features.Capability>().ToImmutableHashSet()
    );

    public async Task SetUp(MatchInfo matchInfo, CancellationToken? token = null)
    {
        logger.LogInformation("Setting up coinflip match...");
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: token ?? CancellationToken.None);
    }

    public async Task<MatchResult> Perform(CancellationToken? token = null)
    {
        logger.LogInformation("Performing coinflip match...");
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken: token ?? CancellationToken.None);
        return Random.Next(2) == 0 ? MatchResult.Blue : MatchResult.Red;
    }
}
