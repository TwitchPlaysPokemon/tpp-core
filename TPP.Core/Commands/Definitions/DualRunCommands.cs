using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class DualRunCommands : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("left", ctx => PickSide(ctx, "left"))
        {
            Aliases = new[] { "blue", "corvus" },
            Description = "Makes your inputs go to the left side team."
        },
        new Command("right", ctx => PickSide(ctx, "right"))
        {
            Aliases = new[] { "red", "greene" },
            Description = "Makes your inputs go to the right side team."
        },
        new Command("noside", ctx => PickSide(ctx, null))
        {
            Description = "Clears your input side selection.",
            Aliases = new[] { "unselectside" },
        },
    }.Select(c => c.WithChangedDescription(desc => desc + " Use !left, !right or !noside to change your selection."));

    private readonly IInputSidePicksRepo _inputSidePicksRepo;
    private readonly IClock _clock;
    private readonly Func<TimeSpan?> _sidePickCooldownProvider;

    public DualRunCommands(
        IInputSidePicksRepo inputSidePicksRepo, IClock clock, Func<TimeSpan?> sidePickCooldownProvider)
    {
        _inputSidePicksRepo = inputSidePicksRepo;
        _clock = clock;
        _sidePickCooldownProvider = sidePickCooldownProvider;
    }

    private async Task<CommandResult> PickSide(CommandContext context, string? side)
    {
        SidePick? sidePick = await _inputSidePicksRepo.GetSidePick(context.Message.User.Id);
        if (sidePick != null && sidePick.Side == side)
            return new CommandResult { Response = "You already selected that side" };
        TimeSpan? sidePickCooldown = _sidePickCooldownProvider();
        if (sidePickCooldown != null && sidePick != null)
        {
            Instant cooldownExpiresAt = sidePick.PickedAt + Duration.FromTimeSpan(sidePickCooldown.Value);
            Duration remainingCooldown = cooldownExpiresAt - _clock.GetCurrentInstant();
            if (remainingCooldown > Duration.Zero)
                return new CommandResult
                {
                    Response = "You can change teams again in " +
                               $"{remainingCooldown.ToTimeSpan().ToHumanReadable(FormatPrecision.Seconds)}"
                };
        }
        await _inputSidePicksRepo.SetSide(context.Message.User.Id, side);
        return new CommandResult { Response = side == null ? "Unselected side" : $"Selected the {side} side team" };
    }
}
