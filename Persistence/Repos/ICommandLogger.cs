using System.Collections.Immutable;
using System.Threading.Tasks;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface ICommandLogger
    {
        public Task<CommandLog> Log(string userId, string command, IImmutableList<string> args, string? response);
    }
}
