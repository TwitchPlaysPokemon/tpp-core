using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IPollRepo
    {
        public Task<Poll> CreatePoll(string pollName, string pollCode, bool multiChoice, string[] pollOptions);
        public Task<Poll> Vote(string id, string userId, int[] options);
        public Task<bool> IsPollValid(string pollCode);
        public Task<bool> IsVoteValid(string pollCode, int[] votes);
        public Task<bool> IsMulti(string pollCode);
        public Task<bool> HasVoted(string pollCode, string userId);
    }
}
