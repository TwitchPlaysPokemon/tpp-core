using System.Threading.Tasks;
using NodaTime;

namespace Core.Commands
{
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

        /// Replace the command execution with one that does nothing
        /// if the command was recently executed within a given time span.
        /// The cooldown is applied globally
        public static Command WithGlobalCooldown(this Command command, Duration duration)
        {
            var cooldown = new GlobalCooldown(SystemClock.Instance, duration);
            return new Command(command.Name,
                    ctx => cooldown.CheckLapsedThenReset()
                        ? command.Execution(ctx)
                        : Task.FromResult(new CommandResult()))
            { Aliases = command.Aliases, Description = command.Description };
        }

        /// Replace the command execution with one that does nothing
        /// if the command was recently executed within a given time span.
        /// The cooldown is applied per user.
        public static Command WithPerUserCooldown(this Command command, Duration duration)
        {
            var cooldown = new PerUserCooldown(SystemClock.Instance, duration);
            return new Command(command.Name,
                    ctx => cooldown.CheckLapsedThenReset(ctx.Message.User)
                        ? command.Execution(ctx)
                        : Task.FromResult(new CommandResult()))
            { Aliases = command.Aliases, Description = command.Description };
        }
    }
}
