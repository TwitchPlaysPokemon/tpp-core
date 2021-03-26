using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;
using static TPP.Core.Commands.UserGroup;

namespace TPP.Core.Commands.Definitions
{
    public class StopToken
    {
        public bool ShouldStop { get; set; }
    }

    public class OperatorCommands : ICommandCollection
    {
        private readonly StopToken _stopToken;
        private readonly IBank<User> _pokeyenBank;
        private readonly IBank<User> _tokensBank;
        private readonly IMessageSender _messageSender;
        private readonly IBadgeRepo _badgeRepo;
        private readonly IUserRepo _userRepo;

        public OperatorCommands(
            StopToken stopToken,
            IBank<User> pokeyenBank,
            IBank<User> tokensBank,
            IMessageSender messageSender,
            IBadgeRepo badgeRepo,
            IUserRepo userRepo)
        {
            _stopToken = stopToken;
            _pokeyenBank = pokeyenBank;
            _tokensBank = tokensBank;
            _messageSender = messageSender;
            _badgeRepo = badgeRepo;
            _userRepo = userRepo;
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("stopnew", Stop, UserGroup.Operator)
            {
                Description = "Operators only: Stop the core, or cancel a previously issued stop command. " +
                              "Argument: cancel(optional)"
            },
            new Command("pokeyenadjust", AdjustPokeyen, UserGroup.Operator)
            {
                Aliases = new[] { "adjustpokeyen" },
                Description = "Operators only: Add or remove pokeyen from an user. " +
                              "Arguments: p<amount>(can be negative) <user> <reason>"
            },
            new Command("tokensadjust", AdjustTokens, UserGroup.Operator)
            {
                Aliases = new[] { "adjusttokens" },
                Description = "Operators only: Add or remove tokens from an user. " +
                              "Arguments: t<amount>(can be negative) <user> <reason>"
            },
            new Command("transferbadge", TransferBadge, UserGroup.Operator)
            {
                Description = "Operators only: Transfer badges from one user to another user. " +
                              "Arguments: <gifter> <recipient> <pokemon> <number of badges>(Optional) <reason>"
            },
            new Command("createbadge", CreateBadge, UserGroup.Operator)
            {
                Description = "Operators only: Create a badge for a user. " +
                              "Arguments: <recipient> <pokemon> <number of badges>(Optional)"
            },
            new Command("setoperator", setOperator, UserGroup.Operator)
            {
                Aliases = new [] { "op", "operator"},
                Description = "Operators only: Give other users operator status. Removes other roles. " +
                              "Arguments: <user> <user> ..."
            },
            new Command("setmoderator", setModerator, UserGroup.Operator)
            {
                Aliases = new [] { "mod", "moderator"},
                Description = "Operators only: Give other users moderator status. Removes other roles. " +
                              "Arguments: <user> <user> ..."
            },
            new Command("settrusted", setTrusted, UserGroup.Operator)
            {
                Aliases = new [] { "trust", "trusted" },
                Description = "Operator only: Give other users trusted status. Removes modteam roles. " +
                              "Arguments: <user> <user> ..."
            },
            new Command("setmusicteam", setMusicTeam, UserGroup.Operator)
            {
                Aliases = new [] { "musicteam" },
                Description = "Operator only: Give other users music team status. Removes modteam roles. " +
                              "Arguments: <user> <user> ..."
            },
            new Command("removeroles", removeRoles, UserGroup.Operator)
            {
                Aliases = new [] { "demote" },
                Description = "Operator only: Remove all roles from other users. " +
                              "Arguments: <user> <user> ..."
            }
        };

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
            (User user, T deltaObj, ManyOf<string> reasonParts) =
                await context.ParseArgs<AnyOrder<User, T, ManyOf<string>>>();
            string reason = string.Join(' ', reasonParts.Values);
            int delta = deltaObj;

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
                if (string.IsNullOrEmpty(reason))
                {
                    return new CommandResult { Response = $"Must provide a reason for the {currencyName} adjustment" };
                }
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

            List<Badge> badges = await _badgeRepo.FindByUserAndSpecies(gifter.Id, species);
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

        public async Task<CommandResult> setOperator(CommandContext context)
        {
            ManyOf<User> toPromote = await context.ParseArgs<ManyOf<User>>();

            foreach (User u in toPromote.Values)
            {
                // operator has all permissions, so other roles are not preserved.
                await _userRepo.SetUserGroup(u, (byte)UserGroup.Operator);
            }

            return new CommandResult
            {
                Response = toPromote.Values.IsEmpty ? "No users found." : "User(s) have been given operator status."
            };
        }

        public async Task<CommandResult> setModerator(CommandContext context)
        {
            ManyOf<User> toPromote = await context.ParseArgs<ManyOf<User>>();

            foreach (User u in toPromote.Values)
            {
                // assumes moderator includes permissions of all other groups excluding operator, and therefore the groups don't need to be preserved
                await _userRepo.SetUserGroup(u, (byte)UserGroup.Moderator);
            }

            return new CommandResult
            {
                Response = toPromote.Values.IsEmpty ? "No users found." : "User(s) have been given moderator status."
            };
        }

        public async Task<CommandResult> setTrusted(CommandContext context)
        {
            ManyOf<User> toPromote = await context.ParseArgs<ManyOf<User>>();

            foreach (User u in toPromote.Values)
            {
                UserGroup newGroup = (UserGroup)u.UserGroup | UserGroup.Trusted; // add trusted
                newGroup -= UserGroup.ModTeam & (UserGroup)u.UserGroup; // strip moderator and operator ranks if they are present
                await _userRepo.SetUserGroup(u, (byte)newGroup);
            }

            return new CommandResult
            {
                Response = toPromote.Values.IsEmpty ? "No users found." : "User(s) have been given trusted status."
            };
        }

        public async Task<CommandResult> setMusicTeam(CommandContext context)
        {
            ManyOf<User> toPromote = await context.ParseArgs<ManyOf<User>>();

            foreach (User u in toPromote.Values)
            {
                UserGroup newGroup = (UserGroup)u.UserGroup | UserGroup.MusicTeam; // add music team
                newGroup -= UserGroup.ModTeam & (UserGroup)u.UserGroup; // strip moderator and operator ranks if they are present
                await _userRepo.SetUserGroup(u, (byte)newGroup);
            }

            return new CommandResult
            {
                Response = toPromote.Values.IsEmpty ? "No users found." : "User(s) have been given music team status."
            };
        }

        public async Task<CommandResult> removeRoles(CommandContext context)
        {
            ManyOf<User> toDemote = await context.ParseArgs<ManyOf<User>>();

            foreach (User u in toDemote.Values)
            {
                await _userRepo.SetUserGroup(u, (byte)UserGroup.None);
            }

            return new CommandResult
            {
                Response = toDemote.Values.IsEmpty ? "No users found." : "User(s) have been stripped of all roles."
            };
        }
    }
}
