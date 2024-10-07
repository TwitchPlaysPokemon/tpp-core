using System;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Moderation;

public enum TimeoutResult { Ok, MustBe2WeeksOrLess, UserIsBanned, UserIsModOrOp, NotSupportedInChannel }
public enum BanResult { Ok, UserIsModOrOp, NotSupportedInChannel }
public enum SetAppealCooldownResult { Ok, UserNotBanned, AlreadyPermanent }
public enum ModerationActionType { Ban, Unban, Timeout, Untimeout }
public class ModerationActionPerformedEventArgs : EventArgs
{
    public User IssuerUser { get; }
    public User TargetUser { get; }
    public ModerationActionType Type { get; }

    public ModerationActionPerformedEventArgs(User issuerUser, User targetUser, ModerationActionType type)
    {
        IssuerUser = issuerUser;
        TargetUser = targetUser;
        Type = type;
    }
}
public class ModerationService
{
    private readonly IClock _clock;
    private readonly IExecutor? _executor;
    private readonly ITimeoutLogRepo _timeoutLogRepo;
    private readonly IBanLogRepo _banLogRepo;
    private readonly IAppealCooldownLogRepo _appealCooldownLogRepo;
    private readonly IUserRepo _userRepo;

    public event EventHandler<ModerationActionPerformedEventArgs>? ModerationActionPerformed;

    public ModerationService(
        IClock clock, IExecutor? executor, ITimeoutLogRepo timeoutLogRepo, IBanLogRepo banLogRepo, IAppealCooldownLogRepo appealCooldownLogRepo, IUserRepo userRepo)
    {
        _clock = clock;
        _executor = executor;
        _timeoutLogRepo = timeoutLogRepo;
        _banLogRepo = banLogRepo;
        _appealCooldownLogRepo = appealCooldownLogRepo;
        _userRepo = userRepo;
    }

    public Task<BanResult> Ban(User issuerUser, User targetUser, string reason) =>
        BanOrUnban(issuerUser, targetUser, reason, true);
    public Task<BanResult> Unban(User issuerUser, User targetUser, string reason) =>
        BanOrUnban(issuerUser, targetUser, reason, false);

    private async Task<BanResult> BanOrUnban(User issuerUser, User targetUser, string reason, bool isBan)
    {
        if (_executor == null)
            return BanResult.NotSupportedInChannel;

        if (targetUser.Roles.Overlaps(new[] { Role.Operator, Role.Moderator }))
            return BanResult.UserIsModOrOp;

        Instant now = _clock.GetCurrentInstant();

        if (isBan)
            await _executor.Ban(targetUser, reason);
        else
            await _executor.Unban(targetUser, reason);

        await _banLogRepo.LogBan(
            targetUser.Id, isBan ? "manual_ban" : "manual_unban", reason,
            issuerUser.Id, now);
        await _timeoutLogRepo.LogTimeout( // bans/unbans automatically lift timeouts
            targetUser.Id, isBan ? "untimeout_from_manual_ban" : "untimeout_from_manual_unban", reason,
            issuerUser.Id, now, null);
        await _userRepo.SetBanned(targetUser, isBan);

        // First ban can be appealed after 1 month by default.
        // I don't want to automatically calculate how many bans the user has had, because some might be joke/mistakes
        // A mod should manually set the next appeal cooldown based on the rules.
        var DEFAULT_APPEAL_TIME = Duration.FromDays(30);
        Instant expiration = now + DEFAULT_APPEAL_TIME;
        await _userRepo.SetAppealCooldown(targetUser, expiration);
        await _appealCooldownLogRepo.LogAppealCooldownChange(targetUser.Id, "auto_appeal_cooldown", issuerUser.Id, now, DEFAULT_APPEAL_TIME);

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
        if (_executor == null)
            return TimeoutResult.NotSupportedInChannel;

        if (targetUser.Roles.Overlaps(new[] { Role.Operator, Role.Moderator }))
            return TimeoutResult.UserIsModOrOp;

        bool isIssuing = duration != null;
        if (duration.HasValue && duration > Duration.FromDays(14))
            return TimeoutResult.MustBe2WeeksOrLess;
        if (targetUser.Banned)
            return TimeoutResult.UserIsBanned;

        Instant now = _clock.GetCurrentInstant();

        if (isIssuing)
            await _executor.Timeout(targetUser, reason, duration!.Value);
        else
            await _executor.Unban(targetUser, reason);
        await _timeoutLogRepo.LogTimeout(
            targetUser.Id, isIssuing ? "manual_timeout" : "manual_untimeout", reason,
            issuerUser.Id, now, duration);
        await _userRepo.SetTimedOut(targetUser, duration.HasValue ? now + duration.Value : null);

        ModerationActionPerformed?.Invoke(this, new ModerationActionPerformedEventArgs(
            issuerUser, targetUser, isIssuing ? ModerationActionType.Timeout : ModerationActionType.Untimeout));

        return TimeoutResult.Ok;
    }

    public Task<SetAppealCooldownResult> SetAppealCooldown(User issueruser, User targetuser, Duration duration) =>
        _SetAppealCooldown(issueruser, targetuser, duration);
    public Task<SetAppealCooldownResult> SetUnappealable(User issueruser, User targetuser) =>
        _SetAppealCooldown(issueruser, targetuser, null);

    private async Task<SetAppealCooldownResult> _SetAppealCooldown(
        User issueruser, User targetUser, Duration? duration)
    {
        if (!targetUser.Banned)
            return SetAppealCooldownResult.UserNotBanned;

        Instant now = _clock.GetCurrentInstant();

        if (duration.HasValue)
        {
            Instant expiration = now + duration.Value;
            await _userRepo.SetAppealCooldown(targetUser, expiration);
            await _appealCooldownLogRepo.LogAppealCooldownChange(targetUser.Id, "manual_cooldown_change", issueruser.Id, now, duration);
        }
        else
        {
            if (targetUser.AppealDate is null)
                return SetAppealCooldownResult.AlreadyPermanent;
            await _userRepo.SetAppealCooldown(targetUser, null);
            await _appealCooldownLogRepo.LogAppealCooldownChange(targetUser.Id, "manual_perma", issueruser.Id, now, null);
        }

        return SetAppealCooldownResult.Ok;
    }
}
