using System.Threading;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Match;

public interface IMatchCycle
{
    /// ID of the game this match cycle is playing
    public GameId GameId { get; }

    /// information on what features the match implementation supports
    public Features.FeatureSet FeatureSet { get; }

    /// Sets up a match for the provided match info,
    /// and returns a delegate to perform the prepared match.
    public Task SetUp(MatchInfo matchInfo, CancellationToken? token = null);

    /// Performs a match that was prepared by a previous call to <see cref="IMatchCycle.SetUp"/>.
    public Task<MatchResult> Perform(CancellationToken? token = null);
}
