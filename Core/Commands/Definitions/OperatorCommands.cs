using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Persistence.Models;

namespace Core.Commands.Definitions
{
    public class StopToken
    {
        public bool ShouldStop { get; set; }
    }

    public class OperatorCommands : ICommandCollection
    {
        private readonly StopToken _stopToken;
        private readonly ImmutableHashSet<string> _operatorNamesLower;

        public OperatorCommands(
            StopToken stopToken,
            IEnumerable<string> operatorNames)
        {
            _stopToken = stopToken;
            _operatorNamesLower = operatorNames.Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("stop", Stop),
        }.Select(cmd => cmd.WithCondition(
            canExecute: ctx => IsOperator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only operators can use that command" }));

        private bool IsOperator(User user) => _operatorNamesLower.Contains(user.SimpleName);

        private Task<CommandResult> Stop(CommandContext context)
        {
            var argSet = context.Args.Select(arg => arg.ToLowerInvariant()).ToHashSet();
            bool cancel = argSet.Remove("cancel");

            if (argSet.Count > 1)
                return Task.FromResult(new CommandResult { Response = "too many arguments" });
            else if (argSet.Count == 0)
                return Task.FromResult(new CommandResult { Response = "must specify 'old' or 'new' core (new core)" });

            if (argSet.First() == "old")
                return Task.FromResult(new CommandResult()); // do nothing silently
            else if (argSet.First() != "new")
                return Task.FromResult(new CommandResult { Response = $"unknown argument '{argSet.First()}'" });

            string message = cancel
                ? _stopToken.ShouldStop
                    ? "cancelled a prior stop command (new core)"
                    : "main loop already not stopping (new core)"
                : _stopToken.ShouldStop
                    ? "main loop already stopping (new core)"
                    : "stopping main loop (new core)";
            _stopToken.ShouldStop = !cancel;
            return Task.FromResult(new CommandResult { Response = message });
        }
    }
}
