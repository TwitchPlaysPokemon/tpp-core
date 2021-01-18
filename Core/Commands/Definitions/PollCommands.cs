using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing.Types;
using Common;
using Persistence.Models;
using Persistence.Repos;

namespace Core.Commands.Definitions
{
    public class PollCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            //new Command("poll", StartPoll)
            //{
            //    Aliases = new[] {"poll"},
            //    Description = "Starts a poll with single choice. Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            //},

            //new Command("multipoll", StartMultiPoll)
            //{
            //    Aliases = new[] {"multipoll"},
            //    Description = "Starts a poll with multiple choice. Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            //},

            new Command("vote", Vote)
            {
                Aliases = new[] {"vote"},
                Description = "Vote on a poll. Argument: <PollCode> <Option1> <OptionX> (optional if multi-choice poll)"
            }
        };


        private readonly IPollRepo _pollRepo;

        public PollCommands(IPollRepo pollRepo)
        {
            _pollRepo = pollRepo;
        }


        //public async Task<CommandResult> StartPoll(CommandContext context)
        //{
        //    var argSet = context.Args.Select(arg => arg.ToUpperInvariant()).ToArray();
        //    if (argSet.Length < 4) return new CommandResult { Response = "too few arguments" };

        //    string pollName = argSet[0];
        //    string pollCode = argSet[1];
        //    var options = argSet.Skip(2).ToArray();

        //    await _pollRepo.CreatePoll(pollName, pollCode, false, options);
        //    return new CommandResult { Response = "Single option poll created" };
        //}


        //public async Task<CommandResult> StartMultiPoll(CommandContext context)
        //{
        //    var argSet = context.Args.Select(arg => arg.ToUpperInvariant()).ToArray();
        //    if (argSet.Length < 4) return new CommandResult { Response = "too few arguments" };

        //    string pollName = argSet[0];
        //    string pollCode = argSet[1];
        //    var options = argSet.Skip(2).ToArray();

        //    await _pollRepo.CreatePoll(pollName, pollCode, true, options);
        //    return new CommandResult { Response = "Multi option poll created" };
        //}


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
            if (!isPollValid) return new CommandResult { Response = $"Poll \"{pollName}\" has ended or could not be found." };
            argSet = argSet.Skip(1).ToArray();
            bool useIntArgSet = false;
            int[] intArgSet = { };

            try
            {
                intArgSet = Array.ConvertAll(argSet, int.Parse);
                useIntArgSet = true;
            }
            catch
            { }

            //
            //Don't allow a user to vote twice
            bool hasVoted = await _pollRepo.HasVoted(pollName, context.Message.User.Id);
            if (hasVoted) return new CommandResult { Response = $"You have already voted on poll \"{pollName}\"." };

            //
            //Validate votes
            bool isVoteValid = false;
            if (useIntArgSet)
                isVoteValid = await _pollRepo.IsVoteValid(pollName, intArgSet);
            else
                isVoteValid = await _pollRepo.IsVoteValid(pollName, argSet);

            if (!isVoteValid) return new CommandResult { Response = $"Invalid option included for poll: \"{pollName}\"." };

            //
            //Only allow multiple votes if is set to multi - option poll
            bool isMulti = await _pollRepo.IsMulti(pollName);
            if (argSet.Length > 1 && !isMulti) return new CommandResult { Response = $"Poll \"{pollName}\" is not a multi-choice poll." };

            //
            //Vote
            if (useIntArgSet)
                await _pollRepo.Vote(pollName, context.Message.User.Id, intArgSet);
            else
                await _pollRepo.Vote(pollName, context.Message.User.Id, argSet);

            return new CommandResult { Response = "voted!" };
        }
    }
}
