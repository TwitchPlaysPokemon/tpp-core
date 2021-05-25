using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Commands.Definitions
{
    class ManagePollCommands : ICommandCollection
    {
        private static readonly Role[] AllowedRoles = { Role.Moderator, Role.Operator };

        public IEnumerable<Command> Commands => new[]
        {
            new Command("createpoll", StartPoll)
            {
                Aliases = new[] { "startpoll" },
                Description = "Starts a poll with single choice. " +
                              "Argument: <PollName> <PollCode> <MultipleChoice> <AllowChangeVote> <Option1> <Option2> <OptionX> (optional). " +
                              "Underscores in the poll name will be replaces with spaces."
            },
        }.Select(cmd => cmd.WithCondition(
            canExecute: ctx => ctx.Message.User.Roles.Intersect(AllowedRoles).Any(),
            ersatzResult: new CommandResult { Response = "Only moderators can manage polls" }));

        private readonly IPollRepo _pollRepo;

        public ManagePollCommands(IPollRepo pollRepo)
        {
            _pollRepo = pollRepo;
        }

        public async Task<CommandResult> StartPoll(CommandContext context)
        {
            (string pollName, string pollCode, bool multiChoice, bool allowChangeVote, ManyOf<string> options) =
                await context.ParseArgs<string, string, bool, bool, ManyOf<string>>();
            if (options.Values.Count < 2) return new CommandResult { Response = "must specify at least 2 options" };
            pollName = pollName.Replace('_', ' ');

            if (await _pollRepo.FindPoll(pollCode) != null)
                return new CommandResult { Response = $"A poll with the code '{pollCode}' already exists." };

            await _pollRepo.CreatePoll(pollCode, pollName, multiChoice, allowChangeVote, options.Values);
            return new CommandResult
            {
                Response =
                    $"Poll '{pollCode}' created: {pollName}" +
                    $" - {(multiChoice ? "multiple-choice" : "single-choice")}" +
                    $" - {(allowChangeVote ? "changeable votes" : "unchangeable votes")}"
            };
        }
    }
}
