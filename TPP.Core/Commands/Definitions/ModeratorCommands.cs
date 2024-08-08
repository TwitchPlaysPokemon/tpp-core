using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Core.Chat;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class ModeratorCommands(
    IChatModeChanger? changer,
    ILinkedAccountRepo linkedAccountRepo,
    IResponseCommandRepo responseCommandRepo,
    ICommandAliasRepo commandAliasRepo) : ICommandCollection
{
    private const string StaticResponsesCommandName = "responses";
    private const string CommandAliasesCommandName = "aliases";

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
        new Command(CommandAliasesCommandName, Aliases)
        {
            Description = "Manage static response commands. " +
                          "Subcommands: add/update <command> <response>, remove <command>, list",
            Aliases = ["alias"]
        },
    }.Select(cmd => cmd.WithModeratorsOnly());

    private async Task<CommandResult> EnableEmoteOnly(CommandContext context)
    {
        if (changer == null)
            return new CommandResult { Response = "Not supported for this channel" };
        await changer.EnableEmoteOnly();
        return new CommandResult();
    }

    private async Task<CommandResult> DisableEmoteOnly(CommandContext context)
    {
        if (changer == null)
            return new CommandResult { Response = "Not supported for this channel" };
        await changer.DisableEmoteOnly();
        return new CommandResult();
    }

    private async Task<CommandResult> LinkAccounts(CommandContext context)
    {
        ManyOf<User> users = await context.ParseArgs<ManyOf<User>>();
        if (users.Values.Count < 2)
            return new CommandResult { Response = "Must link at least 2 accounts" };
        bool success = await linkedAccountRepo.Link(users.Values.Select(u => u.Id).ToImmutableHashSet());
        return new CommandResult
        {
            Response = success ? "Accounts successfully linked" : "Accounts were already linked"
        };
    }

    private async Task<CommandResult> UnlinkAccounts(CommandContext context)
    {
        User user = await context.ParseArgs<User>();
        bool success = await linkedAccountRepo.Unlink(user.Id);
        return new CommandResult
        {
            Response = success ? "Account successfully unlinked" : "Account was not linked in the first place"
        };
    }

    private async Task<CommandResult> CheckLinkAccounts(CommandContext context)
    {
        User user = await context.ParseArgs<User>();
        IImmutableSet<User> links = await linkedAccountRepo.FindLinkedUsers(user.Id);
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
            IImmutableList<ResponseCommand> commands = await responseCommandRepo.GetCommands();
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
            bool wasRemoved = await responseCommandRepo.RemoveCommand(command);
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
            await responseCommandRepo.UpsertCommand(command, response);
            return new CommandResult
            {
                Response = $"Static response command '{command}' was set to response: {response}"
            };
        }
        return new CommandResult { Response = $"Unknown subcommand '{subcommand}'." };
    }

    private async Task<CommandResult> Aliases(CommandContext context)
    {
        if (context.Args.Count == 0)
            return new CommandResult { Response = $"See '!help {CommandAliasesCommandName}' for usage." };
        string subcommand = context.Args[0].ToLower();
        if (subcommand == "list")
        {
            IImmutableList<CommandAlias> aliases = await commandAliasRepo.GetAliases();
            if (aliases.Count == 0)
                return new CommandResult { Response = "There currently are no command aliases." };
            return new CommandResult
            {
                Response = "These command aliases currently exist: " +
                           string.Join(", ", aliases.Select(c => c.Alias + " -> " + c.TargetCommand))
            };
        }
        else if (subcommand == "remove")
        {
            string alias = context.Args[1];
            bool wasRemoved = await commandAliasRepo.RemoveAlias(alias);
            return new CommandResult
            {
                Response = wasRemoved
                    ? $"Command alias '{alias}' was removed."
                    : $"No command alias '{alias}' exists."
            };
        }
        else if (subcommand is "add" or "update")
        {
            string alias = context.Args[1];
            string targetCommand = context.Args[2];
            if (string.IsNullOrWhiteSpace(targetCommand))
                return new CommandResult { Response = "Must provide an alias target for the command." };
            await commandAliasRepo.UpsertAlias(alias, targetCommand);
            return new CommandResult
            {
                Response = $"Command alias '{alias}' was set to redirect to: {targetCommand}"
            };
        }
        return new CommandResult { Response = $"Unknown subcommand '{subcommand}'." };
    }
}
