using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPP.Persistence.Repos;

namespace TPP.Core.Commands.Definitions
{
    class CreatePollCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            new Command("poll", StartPoll)
            {
                Aliases = new[] { "poll" },
                Description = "Starts a poll with single choice. " +
                              "Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            },

            new Command("multipoll", StartMultiPoll)
            {
                Aliases = new[] { "multipoll" },
                Description = "Starts a poll with multiple choice. " +
                              "Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            },
        };

        private readonly IPollRepo _pollRepo;

        public CreatePollCommands(IPollRepo pollRepo)
        {
            _pollRepo = pollRepo;
        }

        public async Task<CommandResult> StartPoll(CommandContext context)
        {
            var argSet = context.Args.Select(arg => arg.ToUpperInvariant()).ToArray();
            if (argSet.Length < 4) return new CommandResult { Response = "too few arguments" };

            string pollName = argSet[0];
            string pollCode = argSet[1];
            var options = argSet.Skip(2).ToArray();

            await _pollRepo.CreatePoll(pollName, pollCode, false, options);
            return new CommandResult { Response = "Single option poll created" };
        }

        public async Task<CommandResult> StartMultiPoll(CommandContext context)
        {
            var argSet = context.Args.Select(arg => arg.ToUpperInvariant()).ToArray();
            if (argSet.Length < 4) return new CommandResult { Response = "too few arguments" };

            string pollName = argSet[0];
            string pollCode = argSet[1];
            var options = argSet.Skip(2).ToArray();

            await _pollRepo.CreatePoll(pollName, pollCode, true, options);
            return new CommandResult { Response = "Multi option poll created" };
        }
    }
}
