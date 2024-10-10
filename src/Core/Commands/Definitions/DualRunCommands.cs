using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using Common;
using Core.Configuration;
using Model;
using Persistence;

namespace Core.Commands.Definitions;

public class DualRunCommands(Func<RunmodeConfig> runmodeConfigProvider, IInputSidePicksRepo inputSidePicksRepo, IClock clock)
    : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("left", ctx => PickSide(ctx, "left"))
        {
            Description = "Makes your inputs go to the left side team."
        },
        new Command("right", ctx => PickSide(ctx, "right"))
        {
            Description = "Makes your inputs go to the right side team."
        },
        new Command("noside", ctx => PickSide(ctx, null))
        {
            Aliases = ["unselectside"],
            Description = "Clears your input side selection.",
        },
    }.Select(c => c.WithChangedDescription(desc => desc + " Use !left, !right or !noside to change your selection."));

    private async Task<CommandResult> PickSide(CommandContext context, string? side)
    {
        RunmodeConfig runmodeConfig = runmodeConfigProvider();
        if (!runmodeConfig.InputConfig.ButtonsProfile.IsDual())
            return new CommandResult { Response = "Command only enabled during dual-input runs" };
        SidePick? sidePick = await inputSidePicksRepo.GetSidePick(context.Message.User.Id);
        if (sidePick != null && sidePick.Side == side)
            return new CommandResult { Response = "You already selected that side" };
        TimeSpan? sidePickCooldown = runmodeConfig.SwitchSidesCooldown;
        if (sidePickCooldown != null && sidePick != null)
        {
            Instant cooldownExpiresAt = sidePick.PickedAt + Duration.FromTimeSpan(sidePickCooldown.Value);
            Duration remainingCooldown = cooldownExpiresAt - clock.GetCurrentInstant();
            if (remainingCooldown > Duration.Zero)
                return new CommandResult
                {
                    Response = "You can change teams again in " +
                               $"{remainingCooldown.ToTimeSpan().ToHumanReadable(FormatPrecision.Seconds)}"
                };
        }
        await inputSidePicksRepo.SetSide(context.Message.User.Id, side);
        return new CommandResult { Response = side == null ? "Unselected side" : $"Selected the {side} side team" };
    }
}
