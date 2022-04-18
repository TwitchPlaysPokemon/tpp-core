using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class InputtingCommands : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("left", ctx => PickSide(ctx, "left")) { Description = "Makes your inputs go to the left side." },
        new Command("right", ctx => PickSide(ctx, "right")) { Description = "Makes your inputs go to the right side." },
        new Command("noside", ctx => PickSide(ctx, null))
        {
            Description = "Clears your input side selection.",
            Aliases = new[] { "unselectside" },
        },
    }.Select(c => c.WithChangedDescription(desc => desc + " Use !left, !right or !noside to change your selection."));

    private readonly IInputSidePicksRepo _inputSidePicksRepo;
    private readonly IClock _clock;
    private readonly Duration? _sidePickCooldown;

    public InputtingCommands(IInputSidePicksRepo inputSidePicksRepo, IClock clock, Duration? sidePickCooldown)
    {
        _inputSidePicksRepo = inputSidePicksRepo;
        _clock = clock;
        _sidePickCooldown = sidePickCooldown;
    }

    private async Task<CommandResult> PickSide(CommandContext context, string? side)
    {
        SidePick? sidePick = await _inputSidePicksRepo.GetSidePick(context.Message.User.Id);
        if (sidePick != null && sidePick.Side == side)
            return new CommandResult { Response = "You already selected that side" };
        if (_sidePickCooldown != null && sidePick != null)
        {
            Duration remainingCooldown = sidePick.PickedAt + _sidePickCooldown.Value - _clock.GetCurrentInstant();
            if (remainingCooldown > Duration.Zero)
                return new CommandResult
                {
                    Response = "You can change teams again in " +
                               $"{remainingCooldown.ToTimeSpan().ToHumanReadable(FormatPrecision.Seconds)}"
                };
        }
        await _inputSidePicksRepo.SetSide(context.Message.User.Id, side);
        return new CommandResult { Response = side == null ? "Unselected side" : $"Selected side '{side}'" };
    }
}
