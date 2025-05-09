using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

class ManagePollCommands(IPollRepo pollRepo) : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("createpoll", StartPoll)
        {
            Aliases = ["startpoll"],
            Description = "Starts a new poll. " +
                          "Arguments: <PollName> <PollCode> <MultipleChoice> <AllowChangeVote> <Option1> <Option2> <OptionX> (optional). " +
                          "Underscores in the poll name and options will be replaced with spaces."
        },
        new Command("closepoll", ClosePoll)
        {
            Aliases = ["endpoll"],
            Description = "End a poll. Arguments: <PollCode>"
        },
    }.Select(cmd => cmd.WithModeratorsOnly());

    private static string UnderscoresToSpaces(string str)
    {
        // This doesn't account for escaping the escape character '\' itself, but it's good enough
        return new Regex(@"(?<!\\)_").Replace(str, " ").Replace("\\_", "_");
    }

    public async Task<CommandResult> StartPoll(CommandContext context)
    {
        (string pollName, string pollCode, bool multiChoice, bool allowChangeVote, ManyOf<string> optionsArgs) =
            await context.ParseArgs<string, string, bool, bool, ManyOf<string>>();

        ImmutableList<string> options = optionsArgs.Values
            .Select(str => UnderscoresToSpaces(str.ToLower().Trim()))
            .Distinct().ToImmutableList();
        if (optionsArgs.Values.Count > options.Count)
            return new CommandResult { Response = "Options must be case-insensitively unique" };

        if (options.Count < 2) return new CommandResult { Response = "must specify at least 2 options" };
        pollName = UnderscoresToSpaces(pollName);

        if (await pollRepo.FindPoll(pollCode) != null)
            return new CommandResult { Response = $"A poll with the code '{pollCode}' already exists." };

        await pollRepo.CreatePoll(pollCode, pollName, multiChoice, allowChangeVote, options);
        return new CommandResult
        {
            Response =
                $"Poll '{pollCode}' created: {pollName}" +
                $" - {(multiChoice ? "multiple-choice" : "single-choice")}" +
                $" - {(allowChangeVote ? "changeable votes" : "unchangeable votes")}"
        };
    }

    private async Task<CommandResult> ClosePoll(CommandContext context)
    {
        string pollCode = await context.ParseArgs<string>();
        bool? wasAlive = await pollRepo.SetAlive(pollCode, false);
        string response = wasAlive switch
        {
            true => $"The poll '{pollCode}' has been closed.",
            false => $"The poll '{pollCode}' was already closed",
            null => $"No poll with the code '{pollCode}' was found."
        };
        return new CommandResult { Response = response };
    }
}
