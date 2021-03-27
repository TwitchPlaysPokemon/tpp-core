using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IPollRepo
    {
        public Task<Poll> CreatePoll(string pollName, string pollCode, bool multiChoice, string[] pollOptions);
        public Task<Poll> Vote(string id, string userId, string[] options, bool useIntArgs);
        public Task<bool> IsPollValid(string pollCode);
        public Task<bool> IsVoteValid(string pollCode, string[] votes, bool useIntArgs);
        public Task<bool> IsMulti(string pollCode);
        public Task<bool> HasVoted(string pollCode, string userId);
    }
}
