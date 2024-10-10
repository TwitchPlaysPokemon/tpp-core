using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using ArgsParsing.Types;
using Common;
using Model;
using Persistence;

namespace Core.Commands.Definitions;

public class CosmeticsCommands : ICommandCollection
{
    private const string UnlockGlowCommandName = "unlockglow";

    public IEnumerable<Command> Commands => new[]
    {
        new Command("setglow", SetGlow)
        {
            Aliases = new[] { "setsecondarycolor", "setsecondarycolour" },
            Description = "Change the color of the glow around your name as it appears on stream. " +
                          "Argument: #<hexcolor>"
        },
        new Command("removeglow", RemoveGlow)
        {
            Aliases = new[] { "removesecondarycolor", "removesecondarycolour" },
            Description = "Remove the glow around your name as it appears on stream."
        },
        new Command(UnlockGlowCommandName, UnlockGlow)
        {
            Aliases = new[] { "unlocksecondarycolor", "unlocksecondarycolour" },
            Description = "Unlock the ability to change the color of your glow around your name " +
                          "as it appears on stream. Costs 1 token."
        },
        new Command("emblems", CheckEmblems)
        {
            Aliases = new[] { "participation" },
            Description = "Show a user's run participation record. Argument: <username> (optional)"
        }.WithPerUserCooldown(Duration.FromSeconds(3)),
        new Command("selectemblem", SelectEmblem)
        {
            Aliases = new[] { "chooseemblem", "selectparticipationbadge", "chooseparticipationbadge" },
            Description = "Select which run's emblem color to show on stream next to your name. " +
                          "Argument: <run number>"
        },
    };

    private readonly IUserRepo _userRepo;
    private readonly IBank<User> _tokenBank;

    public CosmeticsCommands(IUserRepo userRepo, IBank<User> tokenBank)
    {
        _userRepo = userRepo;
        _tokenBank = tokenBank;
    }

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
        await _userRepo.SetGlowColor(user, color);
        return new CommandResult { Response = $"glow color set to #{color}" };
    }

    public async Task<CommandResult> RemoveGlow(CommandContext context)
    {
        await _userRepo.SetGlowColor(context.Message.User, null);
        return new CommandResult { Response = "your glow color was removed" };
    }

    public async Task<CommandResult> UnlockGlow(CommandContext context)
    {
        User user = context.Message.User;
        if (user.GlowColorUnlocked)
        {
            return new CommandResult { Response = "glow color is already unlocked" };
        }
        if (await _tokenBank.GetAvailableMoney(user) < 1)
        {
            return new CommandResult { Response = "you don't have T1 to unlock the glow color" };
        }
        await _tokenBank.PerformTransaction(new Transaction<User>(user, -1, TransactionType.SecondaryColorUnlock));
        await _userRepo.SetGlowColorUnlocked(user, true);
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
        await _userRepo.SetSelectedEmblem(user, emblem);
        return new CommandResult
        {
            Response = $"color of participation badge {Emblems.FormatEmblem(emblem)} successfully equipped"
        };
    }
}
