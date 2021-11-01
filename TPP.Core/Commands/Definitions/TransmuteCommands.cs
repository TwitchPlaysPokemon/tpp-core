using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Model;

namespace TPP.Core.Commands.Definitions;

public class TransmuteCommands : ICommandCollection
{
    private const int DefaultTransmutationCooldownSeconds = 30;

    private readonly ITransmuter _transmuter;
    private readonly PerUserCooldown _cooldown;

    public IEnumerable<Command> Commands => new[]
    {
        new Command("transmute", Transmute)
        {
            Description = "Transform 3 or more badges into a (usually) rarer badge. Costs one token. " +
                          "Arguments: <several Pokemon>(at least 3) t1"
        },
    };

    public TransmuteCommands(ITransmuter transmuter, Duration? transmutationCooldown = null)
    {
        _transmuter = transmuter;
        _cooldown = new PerUserCooldown(
            SystemClock.Instance,
            transmutationCooldown ?? Duration.FromSeconds(DefaultTransmutationCooldownSeconds));
    }

    public async Task<CommandResult> Transmute(CommandContext context)
    {
        User user = context.Message.User;
        if (!_cooldown.CheckLapsed(user))
            return new CommandResult
            {
                Response = $"You can only transmute every {DefaultTransmutationCooldownSeconds} seconds."
            };
        (Optional<Tokens> tokensArg, ManyOf<PkmnSpecies> speciesArg) =
            await context.ParseArgs<AnyOrder<Optional<Tokens>, ManyOf<PkmnSpecies>>>();
        if (!tokensArg.IsPresent)
            return new CommandResult
            {
                Response = $"Please include payment 'T{ITransmutationCalculator.TransmutationCost}' in your command"
            };
        ImmutableList<PkmnSpecies> speciesList = speciesArg.Values;

        Badge resultBadge;
        try
        {
            resultBadge = await _transmuter.Transmute(context.Message.User, tokensArg.Value.Number, speciesList);
        }
        catch (TransmuteException ex)
        {
            return new CommandResult { Response = ex.Message };
        }
        _cooldown.Reset(user);

        string badgesStr = string.Join(", ", speciesList.Take(speciesList.Count - 1)) + " and " +
                           speciesList.Last();
        return new CommandResult
        {
            Response = $"{user.Name} transmuted {badgesStr}, and the result is {resultBadge.Species}!"
        };
    }
}
