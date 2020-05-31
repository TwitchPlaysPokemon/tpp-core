using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Commands
{
    public struct CommandInfo
    {
        public string Command { get; }
        public string[] Aliases { get; set; }
        public Func<CommandContext, Task<CommandResult>> Execution { get; }
        public string? Description { get; set; }

        public CommandInfo(
            string command,
            Func<CommandContext, Task<CommandResult>> execution)
        {
            Command = command;
            Execution = execution;
            Aliases = new string[] { };
            Description = null;
        }

        public override string ToString() => Aliases.Any()
            ? $"{Command}({string.Join('/', Aliases)}): {Description ?? "<no description>"}"
            : $"{Command}: {Description ?? "<no description>"}";
    }
}
