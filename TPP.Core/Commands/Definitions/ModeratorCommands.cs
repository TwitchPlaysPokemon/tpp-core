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
    public class ModeratorCommands : ICommandCollection
    {
        private readonly IChatModeChanger _changer;
        private readonly ILinkedAccountRepo _linkedAccountRepo;

        public ModeratorCommands(IChatModeChanger changer,
            ILinkedAccountRepo linkedAccountRepo)
        {
            _changer = changer;
            _linkedAccountRepo = linkedAccountRepo;
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("emoteonly", EnableEmoteOnly)
            {
                Aliases = new[] { "emoteonlyon" },
                Description = "Moderators only: Set the chat to emote only mode."
            },
            new Command("emoteonlyoff", DisableEmoteOnly)
            {
                Description = "Moderators only: Disable emote only mode."
            },
            new Command("linkaccounts", LinkAccounts)
            {
                Aliases = new[] { "linkaccount" },
                Description = "Moderators only: link 2 accounts together to enforce restrictions on alt accounts. " +
                              "Arguments: <target users> (any number, at least 2)"
            },
            new Command("unlinkaccounts", UnlinkAccounts)
            {
                Aliases = new[] { "unlinkaccount" },
                Description = "Moderators only: undo linkaccount for one user. Arguments: <target user>"
            },
            new Command("checklinkaccount", CheckLinkAccounts)
            {
                Aliases = new[] { "checklink", "checkalt", "checklinkedaccount" },
                Description = "Moderators only: Check the accounts linked to a user. Arguments: <target user>"
            },
        }.Select(cmd => cmd.WithCondition(
            canExecute: ctx => IsModerator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only moderators can use that command" }));

        private bool IsModerator(User u)
        {
            return u.Roles == null ? false : u.Roles.Contains(Role.Moderator) || u.Roles.Contains(Role.Operator);
        }

        private async Task<CommandResult> EnableEmoteOnly(CommandContext context)
        {
            await _changer.EnableEmoteOnly();
            return new CommandResult();
        }

        private async Task<CommandResult> DisableEmoteOnly(CommandContext context)
        {
            await _changer.DisableEmoteOnly();
            return new CommandResult();
        }

        private async Task<CommandResult> LinkAccounts(CommandContext context)
        {
            ManyOf<User> users = await context.ParseArgs<ManyOf<User>>();
            if (users.Values.Count < 2)
                return new CommandResult { Response = "Must link at least 2 accounts" };
            bool success = await _linkedAccountRepo.Link(users.Values.Select(u => u.Id).ToImmutableHashSet());
            return new CommandResult
            {
                Response = success ? "Accounts successfully linked" : "Accounts were already linked"
            };
        }

        private async Task<CommandResult> UnlinkAccounts(CommandContext context)
        {
            User user = await context.ParseArgs<User>();
            bool success = await _linkedAccountRepo.Unlink(user.Id);
            return new CommandResult
            {
                Response = success ? "Account successfully unlinked" : "Account was not linked in the first place"
            };
        }

        private async Task<CommandResult> CheckLinkAccounts(CommandContext context)
        {
            User user = await context.ParseArgs<User>();
            IImmutableSet<User> links = await _linkedAccountRepo.FindLinkedUsers(user.Id);
            return new CommandResult
            {
                Response = links.Count > 0
                    ? $"These accounts are all linked: {string.Join(", ", links.Select(u => u.Name))}"
                    : $"{user.Name} is not linked to anyone."
            };
        }
    }
}
