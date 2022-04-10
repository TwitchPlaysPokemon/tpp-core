using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class InputtingCommands : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("left", ctx => PickSide(ctx, "left")) { Description = "Makes your inputs go to the left side." },
        new Command("right", ctx => PickSide(ctx, "right")) { Description = "Makes your inputs go to the right side." },
        new Command("noside", ctx => PickSide(ctx, null)) { Description = "Clears your input side selection."},
    }.Select(c => c.WithChangedDescription(desc => desc + " Use !left, !right or !noside to change your selection."));

    private readonly IInputSidePicksRepo _inputSidePicksRepo;

    public InputtingCommands(IInputSidePicksRepo inputSidePicksRepo) => _inputSidePicksRepo = inputSidePicksRepo;

    private async Task<CommandResult> PickSide(CommandContext context, string? side)
    {
        await _inputSidePicksRepo.SetSide(context.Message.User.Id, side);
        return new CommandResult { Response = $"Selected side '{side}'" };
    }
}
