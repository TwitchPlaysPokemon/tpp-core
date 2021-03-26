using System;
using System.Linq;
using System.Threading.Tasks;

namespace TPP.Core.Commands
{
    public struct Command
    {
        public delegate Task<CommandResult> Execute(CommandContext context);

        public string Name { get; }
        public string[] Aliases { get; init; }
        public Execute Execution { get; }
        public string? Description { get; init; }
        public UserGroup RequiredRank { get; init; }

        public Command(
            string name,
            Execute execution,
            UserGroup requiredRank=UserGroup.None)
        {
            Name = name;
            Execution = execution;
            Aliases = Array.Empty<string>();
            Description = null;
            RequiredRank = requiredRank;
        }

        public override string ToString() => Aliases.Any()
            ? $"{Name}({string.Join('/', Aliases)}): {Description ?? "<no description>"}"
            : $"{Name}: {Description ?? "<no description>"}";
    }
}
