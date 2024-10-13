using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Core.Moderation;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class ModerationCommands : ICommandCollection
{
    private readonly ModerationService _moderationService;
    private readonly IBanLogRepo _banLogRepo;
    private readonly ITimeoutLogRepo _timeoutLogRepo;
    private readonly IUserRepo _userRepo;
    private readonly IAppealCooldownLogRepo _appealCooldownRepo;
    private readonly IClock _clock;

    public ModerationCommands(
        ModerationService moderationService, IBanLogRepo banLogRepo, ITimeoutLogRepo timeoutLogRepo, IAppealCooldownLogRepo appealCooldownRepo, IUserRepo userRepo,
        IClock clock)
    {
        _moderationService = moderationService;
        _banLogRepo = banLogRepo;
        _timeoutLogRepo = timeoutLogRepo;
        _appealCooldownRepo = appealCooldownRepo;
        _userRepo = userRepo;
        _clock = clock;
    }

    public IEnumerable<Command> Commands => new[]
    {
        new Command("ban", Ban),
        new Command("unban", Unban),
        new Command("checkban", CheckBan),
        new Command("timeout", TimeoutCmd),
        new Command("untimeout", UntimeoutCmd),
        new Command("checktimeout", CheckTimeout),
        new Command("setappealcooldown", SetAppealCooldown),
        new Command("setunappealable", SetUnappealable),
        new Command("checkappealcooldown", CheckAppealCooldown),
    }.Select(cmd => cmd.WithModeratorsOnly());

    private static string ParseReasonArgs(ManyOf<string> reasonParts)
    {
        string reason = string.Join(' ', reasonParts.Values);
        if (reason.Length > 150)
            throw new ArgsParseFailure(ImmutableList.Create(new Failure(ErrorRelevanceConfidence.Likely,
                "That reason is too long. Consider using a link to pastebin or something similar.")));
        return reason;
    }

    private async Task<CommandResult> Ban(CommandContext context)
    {
        (User targetUser, ManyOf<string> reasonParts) = await context.ParseArgs<User, ManyOf<string>>();
        string reason = ParseReasonArgs(reasonParts);
        return new CommandResult
        {
            Response = await _moderationService.Ban(context.Message.User, targetUser, reason) switch
            {
                BanResult.Ok => $"Banned {targetUser.Name}. Reason: {reason}",
                BanResult.UserIsModOrOp => "User is a moderator or operator, they cannot be banned.",
                BanResult.NotSupportedInChannel => "Not supported for this channel.",
            }
        };
    }

    private async Task<CommandResult> Unban(CommandContext context)
    {
        (User targetUser, ManyOf<string> reasonParts) = await context.ParseArgs<User, ManyOf<string>>();
        string reason = ParseReasonArgs(reasonParts);
        return new CommandResult
        {
            Response = await _moderationService.Unban(context.Message.User, targetUser, reason) switch
            {
                BanResult.Ok => $"Unbanned {targetUser.Name}. Reason: {reason}",
                BanResult.UserIsModOrOp => "User is a moderator or operator, they cannot be banned.",
                BanResult.NotSupportedInChannel => "Not supported for this channel.",
            }
        };
    }

    private async Task<CommandResult> CheckBan(CommandContext context)
    {
        User targetUser = await context.ParseArgs<User>();
        BanLog? recentLog = await _banLogRepo.FindMostRecent(targetUser.Id);
        string? issuerName = recentLog?.IssuerUserId == null
            ? "<automated>"
            : (await _userRepo.FindById(recentLog.IssuerUserId))?.Name;
        string infoText = recentLog == null
            ? "No ban logs available."
            : $"Last action was {recentLog.Type} by {issuerName} " +
              $"at {recentLog.Timestamp} with reason {recentLog.Reason}";
        return new CommandResult
        {
            Response = targetUser.Banned
                ? $"{targetUser.Name} is banned. {infoText}"
                : $"{targetUser.Name} is not banned. {infoText}"
        };
    }

    private async Task<CommandResult> TimeoutCmd(CommandContext context)
    {
        (User targetUser, TimeSpan timeSpan, ManyOf<string> reasonParts) =
            await context.ParseArgs<User, TimeSpan, ManyOf<string>>();
        Duration duration = Duration.FromTimeSpan(timeSpan);
        string reason = ParseReasonArgs(reasonParts);
        return new CommandResult
        {
            Response = await _moderationService.Timeout(context.Message.User, targetUser, reason, duration) switch
            {
                TimeoutResult.Ok =>
                    $"Timed out {targetUser.Name} for {duration.ToTimeSpan().ToHumanReadable()}. Reason: {reason}",
                TimeoutResult.MustBe2WeeksOrLess => "Twitch timeouts must be 2 weeks or less.",
                TimeoutResult.UserIsBanned => "User is banned. Unban them first to issue a timeout.",
                TimeoutResult.UserIsModOrOp => "User is a moderator or operator, they cannot be timed out.",
                TimeoutResult.NotSupportedInChannel => "Not supported for this channel.",
            }
        };
    }

    private async Task<CommandResult> UntimeoutCmd(CommandContext context)
    {
        (User targetUser, ManyOf<string> reasonParts) =
            await context.ParseArgs<User, ManyOf<string>>();
        string reason = ParseReasonArgs(reasonParts);
        return new CommandResult
        {
            Response = await _moderationService.Untimeout(context.Message.User, targetUser, reason) switch
            {
                TimeoutResult.Ok => $"Untimed out {targetUser.Name}. Reason: {reason}",
                TimeoutResult.MustBe2WeeksOrLess => "Twitch timeouts must be 2 weeks or less.",
                TimeoutResult.UserIsBanned => "User is banned. Unban them instead if desired.",
                TimeoutResult.UserIsModOrOp => "User is a moderator or operator, they cannot be timed out.",
                TimeoutResult.NotSupportedInChannel => "Not supported for this channel.",
            }
        };
    }

    private async Task<CommandResult> CheckTimeout(CommandContext context)
    {
        User targetUser = await context.ParseArgs<User>();
        if (targetUser.Banned)
            return new CommandResult { Response = $"{targetUser.Name} is banned. Use `checkban` for more info." };
        TimeoutLog? recentLog = await _timeoutLogRepo.FindMostRecent(targetUser.Id);
        string? issuerName = recentLog?.IssuerUserId == null
            ? "<automated>"
            : (await _userRepo.FindById(recentLog.IssuerUserId))?.Name;
        string infoText = recentLog == null
            ? "No timeout logs available."
            : $"Last action was {recentLog.Type} by {issuerName} " +
              $"at {recentLog.Timestamp} with reason {recentLog.Reason}";
        if (recentLog?.Duration != null) infoText += $" for {recentLog.Duration.Value.ToTimeSpan().ToHumanReadable()}";
        Duration remainingTimeout = targetUser.TimeoutExpiration.HasValue
            ? targetUser.TimeoutExpiration.Value - _clock.GetCurrentInstant()
            : Duration.Zero;
        return new CommandResult
        {
            Response = remainingTimeout > Duration.Zero
                ? $"{targetUser.Name} is timed out for another {remainingTimeout.ToTimeSpan().ToHumanReadable()}. {infoText}"
                : $"{targetUser.Name} is not timed out. {infoText}"
        };
    }

    private async Task<CommandResult> SetAppealCooldown(CommandContext context)
    {
        (User targetUser, TimeSpan timeSpan) =
             await context.ParseArgs<User, TimeSpan>();
        Duration duration = Duration.FromTimeSpan(timeSpan);
        return new CommandResult
        {
            Response = await _moderationService.SetAppealCooldown(context.Message.User, targetUser, duration) switch
            {
                SetAppealCooldownResult.Ok => $"{targetUser.Name} is now eligible to appeal at {_clock.GetCurrentInstant() + duration}.",
                SetAppealCooldownResult.UserNotBanned => $"{targetUser.Name} is not banned.",
                SetAppealCooldownResult.AlreadyPermanent => throw new InvalidOperationException("Unexpected AlreadyPermanent repsonse"),
            }
        };
    }

    private async Task<CommandResult> SetUnappealable(CommandContext context)
    {
        User targetUser = await context.ParseArgs<User>();
        return new CommandResult
        {
            Response = await _moderationService.SetUnappealable(context.Message.User, targetUser) switch
            {
                SetAppealCooldownResult.Ok => $"{targetUser.Name} is now ineligible to appeal their ban.",
                SetAppealCooldownResult.UserNotBanned => $"{targetUser.Name} is not banned.",
                SetAppealCooldownResult.AlreadyPermanent => $"{targetUser.Name} is already ineligible to appeal.",
            }
        };
    }

    private async Task<CommandResult> CheckAppealCooldown(CommandContext context)
    {
        User targetUser = await context.ParseArgs<User>();
        if (!targetUser.Banned)
            return new CommandResult { Response = $"{targetUser.Name} is not banned." };

        AppealCooldownLog? recentLog = await _appealCooldownRepo.FindMostRecent(targetUser.Id);
        string? issuerName = recentLog?.IssuerUserId == null
            ? "<automated>"
            : (await _userRepo.FindById(recentLog.IssuerUserId))?.Name;
        string infoText = recentLog == null
            ? "No logs available."
            : $"Last action was {recentLog.Type} by {issuerName} " +
              $"at {recentLog.Timestamp}";
        if (recentLog?.Duration != null) infoText += $" for {recentLog.Duration.Value.ToTimeSpan().ToHumanReadable()}";

        return new CommandResult { Response =
            targetUser.AppealDate is null
            ? $"{targetUser.Name} is not eligible to appeal. {infoText}"
            : targetUser.AppealDate < _clock.GetCurrentInstant()
            ? $"{targetUser.Name} is eligible to appeal now. {infoText}"
            : $"{targetUser.Name} will be eligible to appeal on {targetUser.AppealDate}. {infoText}"
        };
    }
}
