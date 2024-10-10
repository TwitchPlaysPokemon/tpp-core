using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Model;
using Persistence;

namespace Core.Chat;

/// <summary>
/// Sends out messages that the old core queued in the database.
/// </summary>
public sealed class SendOutQueuedMessagesWorker : IWithLifecycle
{
    private readonly ILogger<SendOutQueuedMessagesWorker> _logger;
    private readonly IIncomingMessagequeueRepo _incomingMessagequeueRepo;
    private readonly IUserRepo _userRepo;
    private readonly IMessageSender _messageSender;
    private readonly IClock _clock;

    public SendOutQueuedMessagesWorker(
        ILogger<SendOutQueuedMessagesWorker> logger,
        IIncomingMessagequeueRepo incomingMessagequeueRepo,
        IUserRepo userRepo,
        IMessageSender messageSender,
        IClock clock)
    {
        _logger = logger;
        _incomingMessagequeueRepo = incomingMessagequeueRepo;
        _userRepo = userRepo;
        _messageSender = messageSender;
        _clock = clock;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        Instant olderThan = _clock.GetCurrentInstant() - Duration.FromMinutes(5);
        await _incomingMessagequeueRepo.Prune(olderThan);
        try
        {
            await _incomingMessagequeueRepo.ForEachAsync(ProcessOnce, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessOnce(IncomingMessagequeueItem item)
    {
        _logger.LogDebug("Received message from queue to send out: {Message}", item);
        if (item.MessageType == MessageType.Chat)
        {
            await _messageSender.SendMessage(item.Message);
        }
        else if (item.MessageType == MessageType.Whisper)
        {
            User? user = await _userRepo.FindById(item.Target);
            if (user == null)
                _logger.LogError(
                    "Cannot send out queued whisper message because User-ID '{UserId}' is unknown: {Message}",
                    item.Target, item);
            else
                await _messageSender.SendWhisper(user, item.Message);
        }
        else
        {
            throw new ArgumentException("Unknown message type {}", item.MessageType.ToString());
        }
    }
}
