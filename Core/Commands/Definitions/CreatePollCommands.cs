using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Commands.Definitions
{
    class CreatePollCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            new Command("poll", StartPoll)
            {
                Aliases = new[] {"poll"},
                Description = "Starts a poll with single choice. Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            },

            new Command("multipoll", StartMultiPoll)
            {
                Aliases = new[] {"multipoll"},
                Description = "Starts a poll with multiple choice. Argument: <PollName> <PollCode> <Option1> <Option2> <OptionX> (optional)"
            },
        }
    }
}
