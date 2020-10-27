using System.Threading;
using System.Threading.Tasks;

namespace Match
{
    public interface IMatchCycle
    {
        /// information on what features the match implementation supports
        public Features.FeatureSet FeatureSet { get; }

        /// Sets up a match for the provided match info,
        /// and returns a delegate to perform the prepared match.
        public Task<Perform> SetUp(MatchInfo matchInfo, CancellationToken? token = null);

        /// A delegate that performs a match that was prepared with <see cref="IMatchCycle.SetUp"/>.
        delegate Task<MatchResult> Perform(CancellationToken? token = null);
    }
}
