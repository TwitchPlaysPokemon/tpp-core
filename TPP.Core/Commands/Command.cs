using System.Threading.Tasks;

namespace TPP.Core.Commands;

public readonly struct Command(
    string name,
    Command.Execute execution)
{
    public delegate Task<CommandResult> Execute(CommandContext context);

    public string Name { get; } = name;
    public string[] Aliases { get; init; } = [];
    public Execute Execution { get; } = execution;
    public string? Description { get; init; } = null;

    public override string ToString() => Aliases.Length > 0
        ? $"{Name}({string.Join('/', Aliases)}): {Description ?? "<no description>"}"
        : $"{Name}: {Description ?? "<no description>"}";
}
