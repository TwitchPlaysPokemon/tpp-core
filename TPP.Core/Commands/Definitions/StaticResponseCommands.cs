using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using static TPP.Core.Commands.CommandUtils;

namespace TPP.Core.Commands.Definitions
{
    public class StaticResponseCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            new Command("reddit", StaticResponse("https://reddit.com/r/twitchplayspokemon"))
                {Description = "Respond with the subreddit."},

            new Command("github", StaticResponse("https://github.com/TwitchPlaysPokemon"))
                {Aliases = new[] {"source"}, Description = "Respond with the github."},

            new Command("rewards", StaticResponse("Season rewards: https://twitchplayspokemon.tv/season_rewards"))
            {
                Aliases = new[] {"seasonrewards"},
                Description = "Respond with the link to the season rewards webpage."
            },

            new Command("leaderboard", StaticResponse("https://twitchplayspokemon.tv/leaderboard"))
                {Description = "Provide a link to the leaderboard website."},

            new Command("w", W)
                {Description = "Tell new players how to whisper."},
        }.Select(cmd => cmd.WithGlobalCooldown(Duration.FromSeconds(10)));

        private static Task<CommandResult> W(CommandContext context)
        {
            string cmdName = context.Args.Count > 1 ? context.Args[1] : "command";
            return Task.FromResult(new CommandResult
            {
                Response = context.Message.MessageSource switch
                {
                    MessageSource.Whisper => "you do not need to use 'w tpp' when you are already whispering.",
                    _ => $"Correct usage is whispering this account with '{cmdName}', or in chat via '!{cmdName}'."
                }
            });
        }
    }
}
