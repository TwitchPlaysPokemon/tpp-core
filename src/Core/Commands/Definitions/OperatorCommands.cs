using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArgsParsing.Types;
using Common;
using Core.Chat;
using Inputting;
using Model;
using Persistence;

namespace Core.Commands.Definitions
{
    public interface IStopToken
    {
        bool IsCancellationRequested();
        void Cancel();
        bool TryUndoCancel();
    }

    public class ToggleableStopToken : IStopToken
    {
        private bool ShouldStop { get; set; }
        public bool IsCancellationRequested() => ShouldStop;
        public void Cancel() => ShouldStop = true;
        public bool TryUndoCancel()
        {
            ShouldStop = false;
            return true;
        }
    }

    public class CancellationStopToken(CancellationTokenSource cancellationTokenSource) : IStopToken
    {
        public bool IsCancellationRequested() => cancellationTokenSource.IsCancellationRequested;
        public void Cancel() => cancellationTokenSource.Cancel();
        public bool TryUndoCancel() => false;
    }

    public class OperatorCommands : ICommandCollection
    {
        private readonly IStopToken _stopToken;
        private readonly MuteInputsToken? _muteInputsToken;
        private readonly IBank<User> _pokeyenBank;
        private readonly IBank<User> _tokensBank;
        private readonly IMessageSender _messageSender;
        private readonly IBadgeRepo _badgeRepo;
        private readonly IUserRepo _userRepo;
        private readonly IInputSidePicksRepo _inputSidePicksRepo;

        public OperatorCommands(
            IStopToken stopToken,
            MuteInputsToken? muteInputsToken,
            IBank<User> pokeyenBank,
            IBank<User> tokensBank,
            IMessageSender messageSender,
            IBadgeRepo badgeRepo,
            IUserRepo userRepo,
            IInputSidePicksRepo inputSidePicksRepo)
        {
            _stopToken = stopToken;
            _muteInputsToken = muteInputsToken;
            _pokeyenBank = pokeyenBank;
            _tokensBank = tokensBank;
            _messageSender = messageSender;
            _badgeRepo = badgeRepo;
            _userRepo = userRepo;
            _inputSidePicksRepo = inputSidePicksRepo;
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("stopnew", Stop)
            {
                Description = "Stop the core, or cancel a previously issued stop command. " +
                              "Argument: cancel(optional)"
            },
            new Command("muteinputs", MuteInputs)
            {
                Description = "Mutes run inputs, disabling run statistics and input execution."
            },
            new Command("unmuteinputs", UnmuteInputs)
            {
                Description = "Unmutes run inputs, enabling run statistics and input execution."
            },
            new Command("pokeyenadjust", AdjustPokeyen)
            {
                Aliases = new[] { "adjustpokeyen" },
                Description = "Add or remove pokeyen from an user. " +
                              "Arguments: p<amount>(can be negative) <user> <reason>"
            },
            new Command("tokensadjust", AdjustTokens)
            {
                Aliases = new[] { "adjusttokens" },
                Description = "Add or remove tokens from an user. " +
                              "Arguments: t<amount>(can be negative) <user> <reason>"
            },
            new Command("transferbadge", TransferBadge)
            {
                Description = "Transfer badges from one user to another user. " +
                              "Arguments: <gifter> <recipient> <pokemon> <number of badges>(Optional) <reason>"
            },
            new Command("createbadge", CreateBadge)
            {
                Description = "Create a badge for a user. " +
                              "Arguments: <recipient> <pokemon> <number of badges>(Optional)"
            },
            new Command("addrole", AddRole)
            {
                Aliases = new[] { "giverole" },
                Description = "Give a user a role. Arguments: <user> <role>"
            },
            new Command("removerole", RemoveRole)
            {
                Description = "Remove a role from a user. Arguments: <user> <role>"
            },
            new Command("clearallsides", ClearAllSidePicks)
            {
                Aliases = new []{"clearallsidepicks"},
                Description = "Clear every user's input side pick."
            },
        }.Select(cmd => cmd.WithOperatorsOnly());

        private Task<CommandResult> Stop(CommandContext context)
        {
            HashSet<string> argSet = context.Args.Select(arg => arg.ToLowerInvariant()).ToHashSet();
            bool cancel = argSet.Remove("cancel");

            if (argSet.Count > 0)
                return Task.FromResult(new CommandResult { Response = "too many arguments" });

            string message;
            if (cancel)
            {
                if (!_stopToken.IsCancellationRequested())
                    message = "main loop already not stopping (new core)";
                else if (!_stopToken.TryUndoCancel())
                    message = "stop request cannot be undone (new core)";
                else
                    message = "cancelled a prior stop command (new core)";
            }
            else
            {
                if (_stopToken.IsCancellationRequested())
                    message = "main loop already stopping (new core)";
                else
                {
                    _stopToken.Cancel();
                    message = "stopping main loop (new core)";
                }
            }
            return Task.FromResult(new CommandResult { Response = message });
        }

        private Task<CommandResult> MuteInputs(CommandContext context)
        {
            if (_muteInputsToken == null)
                return Task.FromResult(new CommandResult { Response = "No input feed that could be muted exists." });
            if (_muteInputsToken.Muted)
                return Task.FromResult(new CommandResult { Response = "The input feed is already muted." });
            _muteInputsToken.Muted = true;
            return Task.FromResult(new CommandResult { Response = "The input feed is now muted." });
        }

        private Task<CommandResult> UnmuteInputs(CommandContext context)
        {
            if (_muteInputsToken == null)
                return Task.FromResult(new CommandResult { Response = "No input feed that could be unmuted exists." });
            if (!_muteInputsToken.Muted)
                return Task.FromResult(new CommandResult { Response = "The input feed is not muted." });
            _muteInputsToken.Muted = false;
            return Task.FromResult(new CommandResult { Response = "The input feed is now not muted anymore." });
        }

        public Task<CommandResult> AdjustPokeyen(CommandContext context)
            => AdjustCurrency<SignedPokeyen>(context, _pokeyenBank, "pokeyen");

        public Task<CommandResult> AdjustTokens(CommandContext context)
            => AdjustCurrency<SignedTokens>(context, _tokensBank, "token");

        private async Task<CommandResult> AdjustCurrency<T>(
            CommandContext context, IBank<User> bank, string currencyName) where T : ImplicitNumber
        {
            (User user, T deltaObj, ManyOf<string> reasonParts) =
                await context.ParseArgs<AnyOrder<User, T, ManyOf<string>>>();
            string reason = string.Join(' ', reasonParts.Values);
            int delta = deltaObj;

            if (string.IsNullOrEmpty(reason))
            {
                return new CommandResult { Response = $"Must provide a reason for the {currencyName} adjustment" };
            }

            var additionalData = new Dictionary<string, object?> { ["responsible_user"] = context.Message.User.Id };
            await bank.PerformTransaction(new Transaction<User>(
                user, delta, TransactionType.ManualAdjustment, additionalData));

            bool isSelf = user == context.Message.User;
            if (isSelf)
            {
                return new CommandResult
                {
                    Response = $"Your {currencyName} balance was adjusted by {delta:+#;-#}. Reason: {reason}"
                };
            }
            else
            {
                await _messageSender.SendWhisper(user,
                    $"{context.Message.User.Name} adjusted your {currencyName} balance by {delta:+#;-#}. Reason: {reason}");
                return new CommandResult
                {
                    Response = $"{user.Name}'s {currencyName} balance was adjusted by {delta:+#;-#}. Reason: {reason}"
                };
            }
        }

        public async Task<CommandResult> TransferBadge(CommandContext context)
        {
            (User gifter, (User recipient, PkmnSpecies species, Optional<PositiveInt> amountOpt),
                    ManyOf<string> reasonParts) =
                await context.ParseArgs<User, AnyOrder<User, PkmnSpecies, Optional<PositiveInt>>, ManyOf<string>>();
            string reason = string.Join(' ', reasonParts.Values);
            int amount = amountOpt.Map(i => i.Number).OrElse(1);

            if (string.IsNullOrEmpty(reason))
                return new CommandResult { Response = "Must provide a reason" };

            if (gifter == context.Message.User)
                return new CommandResult { Response = "Use the regular gift command if you're the gifter" };

            if (recipient == gifter)
                return new CommandResult { Response = "Gifter cannot be equal to recipient" };

            IImmutableList<Badge> badges = await _badgeRepo.FindByUserAndSpecies(gifter.Id, species, amount);
            if (badges.Count < amount)
                return new CommandResult
                {
                    Response =
                        $"You tried to transfer {amount} {species} badges, but the gifter only has {badges.Count}."
                };

            IImmutableList<Badge> badgesToGift = badges.Take(amount).ToImmutableList();
            var data = new Dictionary<string, object?>
            {
                ["gifter"] = gifter.Id,
                ["responsible_user"] = context.Message.User.Id,
                ["reason"] = reason
            };
            await _badgeRepo.TransferBadges(badgesToGift, recipient.Id, BadgeLogType.TransferGiftRemote, data);

            await _messageSender.SendWhisper(recipient, amount > 1
                ? $"{context.Message.User.Name} transferred {amount} {species} badges from {gifter.Name} to you. Reason: {reason}"
                : $"{context.Message.User.Name} transferred a {species} badge from {gifter.Name} to you. Reason: {reason}");
            return new CommandResult
            {
                Response = amount > 1
                    ? $"transferred {amount} {species} badges from {gifter.Name} to {recipient.Name}. Reason: {reason}"
                    : $"transferred a {species} badge from {gifter.Name} to {recipient.Name}. Reason: {reason}",
                ResponseTarget = ResponseTarget.Chat
            };
        }

        public async Task<CommandResult> CreateBadge(CommandContext context)
        {
            (User recipient, PkmnSpecies species, Optional<PositiveInt> amountOpt) =
                await context.ParseArgs<AnyOrder<User, PkmnSpecies, Optional<PositiveInt>>>();
            int amount = amountOpt.Map(i => i.Number).OrElse(1);

            for (int i = 0; i < amount; i++)
                await _badgeRepo.AddBadge(recipient.Id, species, Badge.BadgeSource.ManualCreation);

            return new CommandResult
            {
                Response = amount > 1
                    ? $"{amount} badges of species {species} created for user {recipient.Name}."
                    : $"Badge of species {species} created for user {recipient.Name}."
            };
        }

        public async Task<CommandResult> AddRole(CommandContext context)
        {
            (User user, Role role) = await context.ParseArgs<User, Role>();
            string response;

            HashSet<Role> roles = new HashSet<Role>(user.Roles);
            bool roleAssigned = roles.Add(role);

            if (roleAssigned)
            {
                await _userRepo.SetRoles(user, roles);
                response = $"{user.Name} now has the roles: {string.Join(", ", roles)}";
            }
            else
            {
                response = $"{user.Name} already has the role {role.ToString()}";
            }
            return new CommandResult
            {
                Response = response
            };
        }

        public async Task<CommandResult> RemoveRole(CommandContext context)
        {
            (User user, Role role) = await context.ParseArgs<User, Role>();
            string response;

            HashSet<Role> roles = new HashSet<Role>(user.Roles);
            bool roleRemoved = roles.Remove(role);
            if (roleRemoved)
            {
                await _userRepo.SetRoles(user, roles);
                response = roles.Count > 0
                    ? $"{user.Name} now has the roles: {string.Join(", ", roles)}"
                    : $"{user.Name} now has no roles";
            }
            else
            {
                response = user.Roles.Count > 0
                    ? $"{user.Name} didn't have the role {role.ToString()}. {user.Name}'s roles are: {string.Join(", ", user.Roles)}"
                    : $"{user.Name} has no roles";
            }

            return new CommandResult
            {
                Response = response
            };
        }

        private async Task<CommandResult> ClearAllSidePicks(CommandContext context)
        {
            await _inputSidePicksRepo.ClearAll();
            return new CommandResult { Response = "All side picks cleared." };
        }
    }
}
