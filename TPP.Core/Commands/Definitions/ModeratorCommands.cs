using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Core.Chat;
using TPP.Persistence.Models;

namespace TPP.Core.Commands.Definitions
{
    public class ModeratorCommands : ICommandCollection
    {
        private readonly IChatModeChanger _changer;

        public ModeratorCommands(IChatModeChanger changer)
        {
            _changer = changer;
        }

        public IEnumerable<Command> Commands => new[]
        {
            new Command("emoteonly", EnableEmoteOnly, UserGroup.ModTeam)
            {
                Aliases = new[] { "emoteonlyon" },
                Description = "Moderators only: Set the chat to emote only mode."
            },
            new Command("emoteonlyoff", DisableEmoteOnly, UserGroup.ModTeam)
            {
                Description = "Moderators only: Disable emote only mode."
            }
        };

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
    }
}
