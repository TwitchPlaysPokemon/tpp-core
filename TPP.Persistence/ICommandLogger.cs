using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface ICommandLogger
    {
        public Task<CommandLog> Log(string userId, string command, IImmutableList<string> args, string? response);
    }
}
