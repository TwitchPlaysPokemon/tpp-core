using System;
using System.Linq;
using System.Threading.Tasks;

namespace TPP.Core.Commands;

public readonly struct Command
{
    public delegate Task<CommandResult> Execute(CommandContext context);

    public string Name { get; }
    public string[] Aliases { get; init; }
    public Execute Execution { get; }
    public string? Description { get; init; }

    public Command(
        string name,
        Execute execution)
    {
        Name = name;
        Execution = execution;
        Aliases = Array.Empty<string>();
        Description = null;
    }

    public override string ToString() => Aliases.Any()
        ? $"{Name}({string.Join('/', Aliases)}): {Description ?? "<no description>"}"
        : $"{Name}: {Description ?? "<no description>"}";
}
