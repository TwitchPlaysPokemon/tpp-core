using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IResponseCommandRepo
    {
        /// Get all commands.
        public Task<IImmutableList<ResponseCommand>> GetCommands();

        /// Insert or update a command.
        public Task<ResponseCommand> UpsertCommand(string command, string response);

        /// Remove a command by name. Returns whether that command was found and removed.
        public Task<bool> RemoveCommand(string command);
    }
}
