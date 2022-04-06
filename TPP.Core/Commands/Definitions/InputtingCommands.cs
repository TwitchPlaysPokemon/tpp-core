using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class InputtingCommands : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("left", ctx => PickSide(ctx, "left")),
        new Command("right", ctx => PickSide(ctx, "right")),
    };

    private readonly IInputSidePicksRepo _inputSidePicksRepo;

    public InputtingCommands(IInputSidePicksRepo inputSidePicksRepo) => _inputSidePicksRepo = inputSidePicksRepo;

    private async Task<CommandResult> PickSide(CommandContext context, string side)
    {
        await _inputSidePicksRepo.SetSide(context.Message.User.Id, side);
        return new CommandResult { Response = $"Selected side '{side}'" };
    }
}
