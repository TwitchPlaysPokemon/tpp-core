using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Core.Chat;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Commands.Definitions
{
    public class StopToken
    {
        public bool ShouldStop { get; set; }
    }

    public class OperatorCommands : ICommandCollection
    {
        private readonly StopToken _stopToken;
        private readonly ImmutableHashSet<string> _operatorNamesLower;
        private readonly IBank<User> _pokeyenBank;
        private readonly IBank<User> _tokensBank;
        private readonly IMessageSender _messageSender;

        public OperatorCommands(
            StopToken stopToken,
            IEnumerable<string> operatorNames,
            IBank<User> pokeyenBank,
            IBank<User> tokensBank,
            IMessageSender messageSender)
        {
            _stopToken = stopToken;
            _operatorNamesLower = operatorNames.Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            _pokeyenBank = pokeyenBank;
            _tokensBank = tokensBank;
            _messageSender = messageSender;
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("stopnew", Stop)
            {
                Description = "Operators only: Stop the core, or cancel a previously issued stop command. " +
                              "Argument: cancel(optional)"
            },
            new Command("pokeyenadjust", AdjustPokeyen)
            {
                Aliases = new[] { "adjustpokeyen" },
                Description = "Operators only: Add or remove pokeyen from an user. " +
                              "Arguments: p<amount>(can be negative) <user> <reason>"
            },
            new Command("tokensadjust", AdjustTokens)
            {
                Aliases = new[] { "adjusttokens" },
                Description = "Operators only: Add or remove tokens from an user. " +
                              "Arguments: t<amount>(can be negative) <user> <reason>"
            },
        }.Select(cmd => cmd.WithCondition(
            canExecute: ctx => IsOperator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only operators can use that command" }));

        private bool IsOperator(User user) => _operatorNamesLower.Contains(user.SimpleName);

        private Task<CommandResult> Stop(CommandContext context)
        {
            HashSet<string> argSet = context.Args.Select(arg => arg.ToLowerInvariant()).ToHashSet();
            bool cancel = argSet.Remove("cancel");

            if (argSet.Count > 0)
                return Task.FromResult(new CommandResult { Response = "too many arguments" });

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

        public Task<CommandResult> AdjustPokeyen(CommandContext context)
            => AdjustCurrency<SignedPokeyen>(context, _pokeyenBank, "pokeyen");

        public Task<CommandResult> AdjustTokens(CommandContext context)
            => AdjustCurrency<SignedTokens>(context, _tokensBank, "token");

        private async Task<CommandResult> AdjustCurrency<T>(
            CommandContext context, IBank<User> bank, string currencyName) where T : ImplicitNumber
        {
            (User user, T deltaObj, Optional<string> reason) =
                await context.ParseArgs<AnyOrder<User, T, Optional<string>>>();
            int delta = deltaObj;

            var additionalData = new Dictionary<string, object?> { ["responsible_user"] = context.Message.User.Id };
            await bank.PerformTransaction(new Transaction<User>(
                user, delta, TransactionType.ManualAdjustment, additionalData));

            bool isSelf = user == context.Message.User;
            if (isSelf)
            {
                return new CommandResult
                { Response = $"Your {currencyName} balance was adjusted by {delta:+#;-#}. Reason: {reason}" };
            }
            else
            {
                if (!reason.IsPresent)
                {
                    return new CommandResult { Response = $"Must provide a reason for the {currencyName} adjustment" };
                }
                await _messageSender.SendWhisper(user,
                    $"{context.Message.User.Name} adjusted your {currencyName} balance by {delta:+#;-#}. Reason: {reason}");
                return new CommandResult
                { Response = $"{user.Name}'s {currencyName} balance was adjusted by {delta:+#;-#}. Reason: {reason}" };
            }
        }
    }
}
