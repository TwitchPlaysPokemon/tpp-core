using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Persistence.Repos;

namespace Core.Commands.Definitions
{
    public class PollCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            new Command("vote", Vote)
            {
                Aliases = new[] { "vote" },
                Description = "Vote on a poll. Argument: <PollCode> <Option1> <OptionX> (optional if multi-choice poll)"
            }
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


            //Validate args
            var argSet = context.Args.Select(arg => arg.ToUpperInvariant()).ToArray();
            if (argSet.Length < 2) return new CommandResult { Response = "too few arguments" };

            string pollName = argSet[0];

            //Validate poll
            bool isPollValid = await _pollRepo.IsPollValid(pollName);
            if (!isPollValid)
                return new CommandResult { Response = $"Poll \"{pollName}\" has ended or could not be found." };
            argSet = argSet.Skip(1).ToArray();

            //
            //Don't allow a user to vote twice
            bool hasVoted = await _pollRepo.HasVoted(pollName, context.Message.User.Id);
            if (hasVoted) return new CommandResult { Response = $"You have already voted on poll \"{pollName}\"." };

            //
            //Validate votes
            bool isVoteValid = await _pollRepo.IsVoteValid(pollName, Array.ConvertAll(argSet, int.Parse));

            if (!isVoteValid)
                return new CommandResult { Response = $"Invalid option included for poll: \"{pollName}\"." };

            //
            //Only allow multiple votes if is set to multi - option poll
            bool isMulti = await _pollRepo.IsMulti(pollName);
            if (argSet.Length > 1 && !isMulti)
                return new CommandResult { Response = $"Poll \"{pollName}\" is not a multi-choice poll." };

            //
            //Vote
            await _pollRepo.Vote(pollName, context.Message.User.Id, Array.ConvertAll(argSet, int.Parse));


            return new CommandResult { Response = "voted!" };
        }
    }
}
