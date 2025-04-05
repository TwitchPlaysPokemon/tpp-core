using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class CosmeticsCommands(IUserRepo userRepo, IBank<User> tokenBank) : ICommandCollection
{
    private const string UnlockGlowCommandName = "unlockglow";

    public IEnumerable<Command> Commands =>
    [
        new("setglow", SetGlow)
        {
            Aliases = ["setsecondarycolor", "setsecondarycolour"],
            Description = "Change the color of the glow around your name as it appears on stream. " +
                          "Argument: #<hexcolor>"
        },
        new("removeglow", RemoveGlow)
        {
            Aliases = ["removesecondarycolor", "removesecondarycolour"],
            Description = "Remove the glow around your name as it appears on stream."
        },
        new(UnlockGlowCommandName, UnlockGlow)
        {
            Aliases = ["unlocksecondarycolor", "unlocksecondarycolour"],
            Description = "Unlock the ability to change the color of your glow around your name " +
                          "as it appears on stream. Costs 1 token."
        },
        new Command("emblems", CheckEmblems)
        {
            Aliases = ["participation"],
            Description = "Show a user's run participation record. Argument: <username> (optional)"
        }.WithPerUserCooldown(Duration.FromSeconds(3)),
        new("selectemblem", SelectEmblem)
        {
            Aliases = ["chooseemblem", "selectparticipationbadge", "chooseparticipationbadge"],
            Description = "Select which run's emblem color to show on stream next to your name. " +
                          "Argument: <run number>"
        }
    ];

    public async Task<CommandResult> SetGlow(CommandContext context)
    {
        User user = context.Message.User;
        if (!user.GlowColorUnlocked)
        {
            return new CommandResult
            {
                Response = $"glow color is still locked, use '{UnlockGlowCommandName}' to unlock (costs T1)"
            };
        }
        string color = (await context.ParseArgs<HexColor>()).StringWithoutHash;
        await userRepo.SetGlowColor(user, color);
        return new CommandResult { Response = $"glow color set to #{color}" };
    }

    public async Task<CommandResult> RemoveGlow(CommandContext context)
    {
        await userRepo.SetGlowColor(context.Message.User, null);
        return new CommandResult { Response = "your glow color was removed" };
    }

    public async Task<CommandResult> UnlockGlow(CommandContext context)
    {
        User user = context.Message.User;
        if (user.GlowColorUnlocked)
        {
            return new CommandResult { Response = "glow color is already unlocked" };
        }
        if (await tokenBank.GetAvailableMoney(user) < 1)
        {
            return new CommandResult { Response = "you don't have T1 to unlock the glow color" };
        }
        await tokenBank.PerformTransaction(new Transaction<User>(user, -1, TransactionType.SecondaryColorUnlock));
        await userRepo.SetGlowColorUnlocked(user, true);
        return new CommandResult { Response = "your glow color was unlocked" };
    }

    public async Task<CommandResult> CheckEmblems(CommandContext context)
    {
        var optionalUser = await context.ParseArgs<Optional<User>>();
        bool isSelf = !optionalUser.IsPresent;
        User user = isSelf ? context.Message.User : optionalUser.Value;
        if (user.ParticipationEmblems.Any())
        {
            string formattedEmblems = Emblems.FormatEmblems(user.ParticipationEmblems);
            return new CommandResult
            {
                Response = isSelf
                    ? $"you have participated in the following runs: {formattedEmblems}"
                    : $"{user.Name} has participated in the following runs: {formattedEmblems}",
                ResponseTarget = ResponseTarget.WhisperIfLong
            };
        }
        else
        {
            return new CommandResult
            {
                Response = isSelf
                    ? "you have not participated in any runs"
                    : $"{user.Name} has not participated in any runs"
            };
        }
    }

    public async Task<CommandResult> SelectEmblem(CommandContext context)
    {
        User user = context.Message.User;
        int emblem = await context.ParseArgs<NonNegativeInt>();
        if (!user.ParticipationEmblems.Contains(emblem))
        {
            return new CommandResult { Response = "you don't own that participation badge" };
        }
        await userRepo.SetSelectedEmblem(user, emblem);
        return new CommandResult
        {
            Response = $"color of participation badge {Emblems.FormatEmblem(emblem)} successfully equipped"
        };
    }
}
