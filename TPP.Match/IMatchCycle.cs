using System.Threading;
using System.Threading.Tasks;

namespace TPP.Match
{
    public interface IMatchCycle
    {
        /// information on what features the match implementation supports
        public Features.FeatureSet FeatureSet { get; }

        /// Sets up a match for the provided match info,
        /// and returns a delegate to perform the prepared match.
        public Task SetUp(MatchInfo matchInfo, CancellationToken? token = null);

        /// Performs a match that was prepared by a previous call to <see cref="IMatchCycle.SetUp"/>.
        public Task<MatchResult> Perform(CancellationToken? token = null);
    }
}
