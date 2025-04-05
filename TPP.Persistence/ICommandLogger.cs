using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface ICommandLogger
{
    public Task<CommandLog> Log(string userId, string command, IImmutableList<string> args, string? response);
}
