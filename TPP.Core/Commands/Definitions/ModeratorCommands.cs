using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Core.Chat;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class ModeratorCommands : ICommandCollection
{
    private const string StaticResponsesCommandName = "responses";

    private readonly IChatModeChanger _changer;
    private readonly ILinkedAccountRepo _linkedAccountRepo;
    private readonly IResponseCommandRepo _responseCommandRepo;

    public ModeratorCommands(
        IChatModeChanger changer,
        ILinkedAccountRepo linkedAccountRepo,
        IResponseCommandRepo responseCommandRepo)
    {
        _changer = changer;
        _linkedAccountRepo = linkedAccountRepo;
        _responseCommandRepo = responseCommandRepo;
    }

    public IEnumerable<Command> Commands => new[]
    {
        new Command("emoteonly", EnableEmoteOnly)
        {
            Aliases = new[] { "emoteonlyon" },
            Description = "Set the chat to emote only mode."
        },
        new Command("emoteonlyoff", DisableEmoteOnly)
        {
            Description = "Disable emote only mode."
        },
        new Command("linkaccounts", LinkAccounts)
        {
            Aliases = new[] { "linkaccount" },
            Description = "link 2 accounts together to enforce restrictions on alt accounts. " +
                          "Arguments: <target users> (any number, at least 2)"
        },
        new Command("unlinkaccounts", UnlinkAccounts)
        {
            Aliases = new[] { "unlinkaccount" },
            Description = "undo linkaccount for one user. Arguments: <target user>"
        },
        new Command("checklinkaccount", CheckLinkAccounts)
        {
            Aliases = new[] { "checklink", "checkalt", "checklinkedaccount" },
            Description = "Check the accounts linked to a user. Arguments: <target user>"
        },
        new Command(StaticResponsesCommandName, Responses)
        {
            Description = "Manage static response commands. " +
                          "Subcommands: add/update <command> <response>, remove <command>, list"
        },
    }.Select(cmd => cmd
        .WithCondition(
            canExecute: ctx => IsModerator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only moderators can use that command" })
        .WithChangedDescription(desc => "Moderators only: " + desc)
    );

    private static bool IsModerator(User u) =>
        u.Roles.Contains(Role.Moderator) || u.Roles.Contains(Role.Operator);

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

    private async Task<CommandResult> Responses(CommandContext context)
    {
        if (context.Args.Count == 0)
            return new CommandResult { Response = $"See '!help {StaticResponsesCommandName}' for usage." };
        string subcommand = context.Args[0].ToLower();
        if (subcommand == "list")
        {
            IImmutableList<ResponseCommand> commands = await _responseCommandRepo.GetCommands();
            if (commands.Count == 0)
                return new CommandResult { Response = "There currently are no static response commands." };
            return new CommandResult
            {
                Response = "These static response commands currently exist: " +
                           string.Join(", ", commands.Select(c => c.Command))
            };
        }
        else if (subcommand == "remove")
        {
            string command = context.Args[1];
            bool wasRemoved = await _responseCommandRepo.RemoveCommand(command);
            return new CommandResult
            {
                Response = wasRemoved
                    ? $"Static response command '{command}' was removed."
                    : $"No static response command '{command}' exists."
            };
        }
        else if (subcommand is "add" or "update")
        {
            string command = context.Args[1];
            string response = string.Join(' ', context.Args.Skip(2));
            if (string.IsNullOrWhiteSpace(response))
                return new CommandResult { Response = "Must provide a response text for the command." };
            await _responseCommandRepo.UpsertCommand(command, response);
            return new CommandResult
            {
                Response = $"Static response command '{command}' was set to response: {response}"
            };
        }
        return new CommandResult { Response = $"Unknown subcommand '{subcommand}'." };
    }
}
