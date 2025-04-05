using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class PollCommands : ICommandCollection
{
    private const string PollCommandName = "poll";

    public IEnumerable<Command> Commands => new[]
    {
        new Command("vote", Vote)
        {
            Description =
                "Vote on a poll. Arguments: <PollCode> <Option1> <OptionX> (optional if multi-choice poll). " +
                "Note that votes are not anonymous to avoid fraud."
        },
        new Command(PollCommandName, Poll)
        {
            Aliases = new[] { "checkpoll" },
            Description = "Check a poll's status and options. Argument: <PollCode>"
        },
        new Command("polls", Polls)
        {
            Aliases = new[] { "listpolls", "allpolls", "activepolls" },
            Description = "List all currently active polls."
        },
    };

    private readonly IPollRepo _pollRepo;

    public PollCommands(IPollRepo pollRepo)
    {
        _pollRepo = pollRepo;
    }

    public async Task<CommandResult> Vote(CommandContext context)
    {
        //Use poll code and option to vote.
        //Options can be in ID or String form but must all match
        //Multi-answer polls use spacing between options
        //!vote IDENTIFIER Option2 Option4
        //!vote IDENTIFIER 2 4

        (string pollCode, ManyOf<string> voteStrs) = await context.ParseArgs<string, ManyOf<string>>();
        ImmutableList<string> votes = voteStrs.Values;

        Poll? poll = await _pollRepo.FindPoll(pollCode);
        if (poll == null)
            return new CommandResult { Response = $"No poll with the code '{pollCode}' was found." };

        List<int> selectedOptions = new();
        foreach (string voteStr in votes)
        {
            PollOption? option = null;
            if (int.TryParse(voteStr.TrimStart('#'), out int voteInt))
                option = poll.PollOptions.FirstOrDefault(o => o.Id == voteInt);
            option ??= poll.PollOptions.FirstOrDefault(o =>
                string.Equals(o.Option, voteStr, StringComparison.InvariantCultureIgnoreCase));
            if (option == null)
                return new CommandResult { Response = $"Invalid option '{voteStr}'." };
            selectedOptions.Add(option.Id);
        }

        VoteFailure? failure = await _pollRepo.Vote(
            pollCode, context.Message.User.Id, selectedOptions.ToImmutableList());
        return new CommandResult
        {
            Response = failure switch
            {
                null => "Successfully voted.",
                VoteFailure.PollNotFound => $"No poll with the code '{pollCode}' was found.",
                VoteFailure.PollNotAlive => "The poll has already ended.",
                VoteFailure.AlreadyVoted => "You already voted in that poll.",
                VoteFailure.CannotVoteForNone => poll.MultiChoice
                    ? "Must vote for at least one option."
                    : "Must vote for an option.",
                VoteFailure.NotMultipleChoice => "Cannot select multiple options in non-multi-choice polls.",
                VoteFailure.InvalidOptions { Options: var options } =>
                    $"Invalid poll options: {string.Join(", ", options)}.",
                _ => throw new ArgumentOutOfRangeException(nameof(failure), "Unhandled poll voting result")
            }
        };
    }

    public static string FormatSinglePollAdvertisement(Poll poll)
    {
        string Percentage(PollOption option) => poll.Voters.Count == 0
            ? "0" // avoid division by zero
            : $"{100 * (option.VoterIds.Count / (double)poll.Voters.Count):0.#}";
        IEnumerable<string> results = poll.PollOptions.Select(option =>
            $"#{option.Id} {option.Option} " +
            $"({(option.VoterIds.Count == 1 ? "1 vote" : $"{option.VoterIds.Count} votes")}, " +
            $"{Percentage(option)}%)");
        return $"Poll '{poll.PollCode}': {poll.PollTitle}" +
               $" - {string.Join(", ", results)}" +
               $" - Vote with '!vote {poll.PollCode} <option(s)>'";
    }

    public async Task<CommandResult> Poll(CommandContext context)
    {
        string pollCode = await context.ParseArgs<string>();
        Poll? poll = await _pollRepo.FindPoll(pollCode);
        if (poll == null)
            return new CommandResult { Response = $"No poll with the code '{pollCode}' was found." };
        return new CommandResult { Response = FormatSinglePollAdvertisement(poll) };
    }

    public static string FormatPollsAdvertisement(IImmutableList<Poll> polls) =>
        polls.Any()
            ? $"Currently active polls are: {string.Join(", ", polls.Select(p => p.PollCode))}. " +
              $"Use '!{PollCommandName} <code>' for more details on a specific poll"
            : "There currently are no active polls.";

    private async Task<CommandResult> Polls(CommandContext context)
    {
        IImmutableList<Poll> polls = await _pollRepo.FindPolls(onlyActive: true);
        return new CommandResult { Response = FormatPollsAdvertisement(polls) };
    }
}
