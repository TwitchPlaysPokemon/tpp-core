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

        private bool IsOperator(User user) => _operatorNamesLower.Contains(user.SimpleName.ToLowerInvariant());

        public Task<CommandResult> Stop(CommandContext context)
        {
            _stopToken.ShouldStop = true;
            return Task.FromResult(new CommandResult { Response = "stopping main loop" });
        }
    }
}
