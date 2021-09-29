using System;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing;

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
}
