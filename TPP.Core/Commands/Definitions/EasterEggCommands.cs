using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using static TPP.Core.Commands.CommandUtils;

namespace TPP.Core.Commands.Definitions;

public class EasterEggCommands : ICommandCollection
{
    private static readonly Random Random = new Random();

    public IEnumerable<Command> Commands => new[]
    {
        new Command("racc", RaccAttack)
        {
            Aliases = new[] {"raccattack"},
            Description = "He protecc. He attacc. But most importantly he RaccAttack ."
        },

        new Command("thanks", StaticResponse("You're welcome!"))
            {Aliases = new[] {"thank"}, Description = "Respond to a user's thanks."},

        new Command("twitch", StaticResponse("Kappa"))
            {Description = "Respond with the twitch."},

        new Command("iloveyou", StaticResponse("I love you too! <3"))
            {Aliases = new[] {"iloveyou<3"}, Description = "Respond to a user's love. <3"}
    }.Select(cmd => cmd.WithGlobalCooldown(Duration.FromSeconds(10)));

    public Task<CommandResult> RaccAttack(CommandContext context)
    {
        int rand = Random.Next(8192);
        string response = rand switch
        {
            <= 3 => "tppZig tppZag tppOon",
            <= 11 => "tppZig tppOon",
            <= 27 => "tppZig",
            _ => "RaccAttack"
        };
        return Task.FromResult(new CommandResult { Response = response });
    }
}
