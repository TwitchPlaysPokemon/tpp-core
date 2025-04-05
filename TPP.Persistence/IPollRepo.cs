using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public record VoteFailure
{
    private VoteFailure()
    {
    }

    public sealed record PollNotFound(string PollCode) : VoteFailure;
    public sealed record PollNotAlive : VoteFailure;
    public sealed record AlreadyVoted : VoteFailure;
    public sealed record CannotVoteForNone : VoteFailure;
    public sealed record NotMultipleChoice : VoteFailure;
    public sealed record InvalidOptions(IImmutableList<int> Options) : VoteFailure;
}

public interface IPollRepo
{
    public Task<Poll?> FindPoll(string pollCode);
    public Task<IImmutableList<Poll>> FindPolls(bool onlyActive);
    public Task<Poll> CreatePoll(
        string pollCode, string pollName, bool multiChoice, bool allowChangeVote,
        IImmutableList<string> pollOptions);
    public Task<VoteFailure?> Vote(string id, string userId, IImmutableList<int> options);

    /// <summary>
    /// Sets whether a poll is alive.
    /// </summary>
    /// <param name="id">poll code</param>
    /// <param name="alive">alive bool</param>
    /// <returns>null if the poll was not found, else the previous alive value</returns>
    public Task<bool?> SetAlive(string id, bool alive);
}
