using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Commands
{
    public struct Command
    {
        public string Name { get; }
        public string[] Aliases { get; set; }
        public Func<CommandContext, Task<CommandResult>> Execution { get; }
        public string? Description { get; set; }

        public Command(
            string name,
            Func<CommandContext, Task<CommandResult>> execution)
        {
            Name = name;
            Execution = execution;
            Aliases = new string[] { };
            Description = null;
        }

        public override string ToString() => Aliases.Any()
            ? $"{Name}({string.Join('/', Aliases)}): {Description ?? "<no description>"}"
            : $"{Name}: {Description ?? "<no description>"}";
    }
}
