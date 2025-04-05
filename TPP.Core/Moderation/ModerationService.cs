using System;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Moderation;

public enum TimeoutResult { Ok, MustBe2WeeksOrLess, UserIsBanned, UserIsModOrOp, NotSupportedInChannel }
public enum BanResult { Ok, UserIsModOrOp, NotSupportedInChannel }
public enum ModerationActionType { Ban, Unban, Timeout, Untimeout }
public class ModerationActionPerformedEventArgs(User issuerUser, User targetUser, ModerationActionType type)
    : EventArgs
{
    public User IssuerUser { get; } = issuerUser;
    public User TargetUser { get; } = targetUser;
    public ModerationActionType Type { get; } = type;
}
public class ModerationService(
    IClock clock,
    IExecutor? executor,
    ITimeoutLogRepo timeoutLogRepo,
    IBanLogRepo banLogRepo,
    IUserRepo userRepo)
{
    public event EventHandler<ModerationActionPerformedEventArgs>? ModerationActionPerformed;

    public Task<BanResult> Ban(User issuerUser, User targetUser, string reason) =>
        BanOrUnban(issuerUser, targetUser, reason, true);
    public Task<BanResult> Unban(User issuerUser, User targetUser, string reason) =>
        BanOrUnban(issuerUser, targetUser, reason, false);

    private async Task<BanResult> BanOrUnban(User issuerUser, User targetUser, string reason, bool isBan)
    {
        if (executor == null)
            return BanResult.NotSupportedInChannel;

        if (targetUser.Roles.Overlaps([Role.Operator, Role.Moderator]))
            return BanResult.UserIsModOrOp;

        Instant now = clock.GetCurrentInstant();

        if (isBan)
            await executor.Ban(targetUser, reason);
        else
            await executor.Unban(targetUser, reason);

        await banLogRepo.LogBan(
            targetUser.Id, isBan ? "manual_ban" : "manual_unban", reason,
            issuerUser.Id, now);
        await timeoutLogRepo.LogTimeout( // bans/unbans automatically lift timeouts
            targetUser.Id, isBan ? "untimeout_from_manual_ban" : "untimeout_from_manual_unban", reason,
            issuerUser.Id, now, null);
        await userRepo.SetBanned(targetUser, isBan);

        ModerationActionPerformed?.Invoke(this, new ModerationActionPerformedEventArgs(
            issuerUser, targetUser, isBan ? ModerationActionType.Ban : ModerationActionType.Unban));

        return BanResult.Ok;
    }

    public Task<TimeoutResult> Timeout(User issuerUser, User targetUser, string reason, Duration duration) =>
        TimeoutOrUntimeout(issuerUser, targetUser, reason, duration);
    public Task<TimeoutResult> Untimeout(User issuerUser, User targetUser, string reason) =>
        TimeoutOrUntimeout(issuerUser, targetUser, reason, null);

    private async Task<TimeoutResult> TimeoutOrUntimeout(
        User issuerUser, User targetUser, string reason, Duration? duration)
    {
        if (executor == null)
            return TimeoutResult.NotSupportedInChannel;

        if (targetUser.Roles.Overlaps([Role.Operator, Role.Moderator]))
            return TimeoutResult.UserIsModOrOp;

        bool isIssuing = duration != null;
        if (duration.HasValue && duration > Duration.FromDays(14))
            return TimeoutResult.MustBe2WeeksOrLess;
        if (targetUser.Banned)
            return TimeoutResult.UserIsBanned;

        Instant now = clock.GetCurrentInstant();

        if (isIssuing)
            await executor.Timeout(targetUser, reason, duration!.Value);
        else
            await executor.Unban(targetUser, reason);
        await timeoutLogRepo.LogTimeout(
            targetUser.Id, isIssuing ? "manual_timeout" : "manual_untimeout", reason,
            issuerUser.Id, now, duration);
        await userRepo.SetTimedOut(targetUser, duration.HasValue ? now + duration.Value : null);

        ModerationActionPerformed?.Invoke(this, new ModerationActionPerformedEventArgs(
            issuerUser, targetUser, isIssuing ? ModerationActionType.Timeout : ModerationActionType.Untimeout));

        return TimeoutResult.Ok;
    }
}
