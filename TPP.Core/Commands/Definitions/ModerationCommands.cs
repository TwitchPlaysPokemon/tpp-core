using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing;
using TPP.ArgsParsing.Types;
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
    private readonly IClock _clock;

    public ModerationCommands(
        ModerationService moderationService, IBanLogRepo banLogRepo, ITimeoutLogRepo timeoutLogRepo, IUserRepo userRepo,
        IClock clock)
    {
        _moderationService = moderationService;
        _banLogRepo = banLogRepo;
        _timeoutLogRepo = timeoutLogRepo;
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
    }.Select(cmd => cmd
        .WithCondition(
            canExecute: ctx => IsModerator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only moderators can use that command" })
        .WithChangedDescription(desc => "Moderators only: " + desc)
    );

    private static bool IsModerator(User u) =>
        u.Roles.Contains(Role.Moderator) || u.Roles.Contains(Role.Operator);

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
                _ => throw new ArgumentOutOfRangeException()
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
                _ => throw new ArgumentOutOfRangeException()
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
                TimeoutResult.Ok => $"Timed out {targetUser.Name} for {duration}. Reason: {reason}",
                TimeoutResult.MustBe2WeeksOrLess => "Twitch timeouts must be 2 weeks or less.",
                TimeoutResult.UserIsBanned => "User is banned. Unban them first to issue a timeout.",
                _ => throw new ArgumentOutOfRangeException()
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
                _ => throw new ArgumentOutOfRangeException()
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
        if (recentLog?.Duration != null) infoText += $" for {recentLog.Duration.Value.TotalSeconds}s";
        Duration remainingTimeout = targetUser.TimeoutExpiration.HasValue
            ? targetUser.TimeoutExpiration.Value - _clock.GetCurrentInstant()
            : Duration.Zero;
        return new CommandResult
        {
            Response = remainingTimeout > Duration.Zero
                ? $"{targetUser.Name} is timed out for another {(int)remainingTimeout.TotalSeconds}s. {infoText}"
                : $"{targetUser.Name} is not timed out. {infoText}"
        };
    }
}
