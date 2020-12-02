using System.Collections.Immutable;
using System.Threading.Tasks;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface ICommandLogger
    {
        public Task<CommandLog> Log(User user, string command, IImmutableList<string> args, string? response);
    }
}
