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
    private readonly IUserRepo _userRepo;

    public ModerationCommands(ModerationService moderationService, IBanLogRepo banLogRepo, IUserRepo userRepo)
    {
        _moderationService = moderationService;
        _banLogRepo = banLogRepo;
        _userRepo = userRepo;
    }

    public IEnumerable<Command> Commands => new[]
    {
        new Command("ban", Ban),
        new Command("unban", Unban),
        new Command("checkban", CheckBan),
        new Command("timeout", TimeoutCmd),
        new Command("untimeout", UntimeoutCmd),
        // new Command("checktimeout", ctx => null),
    }.Select(cmd => cmd
        .WithCondition(
            canExecute: ctx => IsStrictlyModerator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only moderators can use that command" })
        .WithChangedDescription(desc => "Moderators only: " + desc)
    );

    /// Unlike regular moderator commands, this purposely excludes operators to prevent abuse.
    private static bool IsStrictlyModerator(User u) => u.Roles.Contains(Role.Moderator);

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
        string infoText = recentLog == null
            ? "No ban logs available."
            : $"Last action was {recentLog.Type} by {_userRepo.FindById(recentLog.IssuerUserId)} " +
              $"at {recentLog.Timestamp} with reason {recentLog.Reason}";
        return new CommandResult { Response = targetUser.Banned
            ? $"{targetUser.Name} is banned. {infoText}"
            : $"{targetUser.Name} is not banned. {infoText}" };
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
}
