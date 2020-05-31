using System;
using System.Threading.Tasks;

namespace Core.Commands
{
    public static class CommandUtils
    {
        public static Func<CommandContext, Task<CommandResult>> StaticResponse(string response)
        {
            return ctx => Task.FromResult(new CommandResult {Response = response});
        }
    }
}
