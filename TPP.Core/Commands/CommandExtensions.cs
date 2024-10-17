using System;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing;
using TPP.Model;

namespace TPP.Core.Commands;

public static class CommandExtensions
{
    public static Task<T1> ParseArgs<T1>(this CommandContext ctx) =>
        ctx.ArgsParser.Parse<T1>(ctx.Args);

    public static Task<(T1, T2)> ParseArgs<T1, T2>(this CommandContext ctx) =>
        ctx.ArgsParser.Parse<T1, T2>(ctx.Args);

    public static Task<(T1, T2, T3)> ParseArgs<T1, T2, T3>(this CommandContext ctx) =>
        ctx.ArgsParser.Parse<T1, T2, T3>(ctx.Args);

    public static Task<(T1, T2, T3, T4)> ParseArgs<T1, T2, T3, T4>(this CommandContext ctx) =>
        ctx.ArgsParser.Parse<T1, T2, T3, T4>(ctx.Args);

    public static Task<(T1, T2, T3, T4, T5)> ParseArgs<T1, T2, T3, T4, T5>(this CommandContext ctx) =>
        ctx.ArgsParser.Parse<T1, T2, T3, T4, T5>(ctx.Args);

    /// Replace the command name with a different one.
    public static Command WithName(this Command command, string newName) =>
        new(newName, command.Execution) { Aliases = command.Aliases, Description = command.Description };

    /// Replace the command execution with a different one.
    public static Command WithExecution(this Command command, Command.Execute newExecution) =>
        new(command.Name, newExecution) { Aliases = command.Aliases, Description = command.Description };

    /// Replace the command execution with one that only executes the original execution
    /// if a condition is met, and returns some ersatz result otherwise.
    public static Command WithCondition(
        this Command command,
        Func<CommandContext, bool> canExecute,
        CommandResult? ersatzResult = null)
    {
        return command.WithExecution(async ctx => canExecute(ctx)
            ? await command.Execution(ctx)
            : ersatzResult ?? new CommandResult());
    }

    /// Prepend a fixed list of args to each command invocation.
    /// E.g. if the fixed args are ["foo", "bar"] and the command gets called with ["baz"],
    /// the execution will receive the arguments ["foo", "bar", "baz"].
    public static Command WithFixedArgs(this Command command, string[] fixedArgs) =>
        command.WithExecution(ctx => command.Execution(ctx with { Args = [..fixedArgs, ..ctx.Args] }));

    /// Replace the command execution with one that does nothing
    /// if the command was recently executed within a given time span.
    /// The cooldown is applied globally.
    public static Command WithGlobalCooldown(this Command command, Duration duration)
    {
        var cooldown = new GlobalCooldown(SystemClock.Instance, duration);
        return command.WithCondition(_ => cooldown.CheckLapsedThenReset());
    }

    /// Replace the command execution with one that does nothing
    /// if the command was recently executed within a given time span.
    /// The cooldown is applied per user.
    public static Command WithPerUserCooldown(this Command command, Duration duration)
    {
        var cooldown = new PerUserCooldown(SystemClock.Instance, duration);
        return command.WithCondition(ctx => cooldown.CheckLapsedThenReset(ctx.Message.User));
    }

    /// Modify the command description through a modification function
    public static Command WithChangedDescription(this Command command, Func<string?, string?> change) =>
        new(command.Name, command.Execution)
        {
            Aliases = command.Aliases,
            Description = change(command.Description)
        };

    public static Command WithModeratorsOnly(this Command command) => command
        .WithCondition(
            canExecute: ctx => IsModerator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only moderators can use that command" })
        .WithChangedDescription(desc => "Moderators only: " + desc);

    public static Command WithOperatorsOnly(this Command command) => command
        .WithCondition(
            canExecute: ctx => IsOperator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only operators can use that command" })
        .WithChangedDescription(desc => "Operators only: " + desc);

    private static bool IsModerator(User user) =>
        user.Roles.Contains(Role.Moderator) || user.Roles.Contains(Role.Operator);

    private static bool IsOperator(User user) =>
        user.Roles.Contains(Role.Operator);
}
