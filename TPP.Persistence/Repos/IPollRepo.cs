using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public record VoteFailure
    {
        private VoteFailure()
        {
        }

        public sealed record PollNotFound(string PollCode) : VoteFailure;
        public sealed record PollNotAlive : VoteFailure;
        public sealed record AlreadyVoted : VoteFailure;
        public sealed record NotMultipleChoice : VoteFailure;
        public sealed record InvalidOptions(IImmutableList<int> Options) : VoteFailure;
    }

    public interface IPollRepo
    {
        public Task<Poll?> FindPoll(string pollCode);

        public Task<Poll> CreatePoll(
            string pollName, string pollCode, bool multiChoice, bool allowChangeVote,
            IImmutableList<string> pollOptions);

        public Task<VoteFailure?> Vote(string id, string userId, IImmutableList<int> options);
    }
}
