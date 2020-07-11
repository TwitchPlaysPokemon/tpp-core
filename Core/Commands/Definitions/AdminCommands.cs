using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing.Types;
using Persistence.Models;

namespace Core.Commands.Definitions
{
    public class StopToken
    {
        public bool ShouldStop { get; set; }
    }

    public class AdminCommands : ICommandCollection
    {
        private readonly StopToken _stopToken;
        private readonly ImmutableHashSet<string> _operatorNamesLower;

        public AdminCommands(
            StopToken stopToken,
            IEnumerable<string> operatorNames)
        {
            _stopToken = stopToken;
            _operatorNamesLower = operatorNames.Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("stop", Stop),
        }.Select(cmd => cmd.WithRestrictedByUser(IsOperator));

        private bool IsOperator(User user) => _operatorNamesLower.Contains(user.SimpleName);

        private async Task<CommandResult> Stop(CommandContext context)
        {
            Optional<string> argument = await context.ParseArgs<Optional<string>>();
            bool cancel = false;
            if (argument.IsPresent)
            {
                bool isCancelArg = argument.Value.ToLowerInvariant() == "cancel";
                if (isCancelArg) cancel = true;
                else return new CommandResult { Response = $"unknown argument '{argument.Value}'" };
            }
            string message = cancel
                ? _stopToken.ShouldStop
                    ? "cancelled a prior stop command"
                    : "main loop already not stopping"
                : _stopToken.ShouldStop
                    ? "main loop already stopping"
                    : "stopping main loop";
            _stopToken.ShouldStop = !cancel;
            return new CommandResult { Response = message };
        }
    }
}
