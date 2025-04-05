using System.Threading.Tasks;

namespace TPP.Core.Commands;

public static class CommandUtils
{
    public static Command.Execute StaticResponse(string response)
    {
        return ctx => Task.FromResult(new CommandResult { Response = response });
    }
}
