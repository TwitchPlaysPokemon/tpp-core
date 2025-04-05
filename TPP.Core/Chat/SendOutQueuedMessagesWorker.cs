using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat;

/// <summary>
/// Sends out messages that the old core queued in the database.
/// </summary>
public sealed class SendOutQueuedMessagesWorker(
    ILogger<SendOutQueuedMessagesWorker> logger,
    IIncomingMessagequeueRepo incomingMessagequeueRepo,
    IUserRepo userRepo,
    IMessageSender messageSender,
    IClock clock)
    : IWithLifecycle
{
    public async Task Start(CancellationToken cancellationToken)
    {
        Instant olderThan = clock.GetCurrentInstant() - Duration.FromMinutes(5);
        await incomingMessagequeueRepo.Prune(olderThan);
        await incomingMessagequeueRepo.ForEachAsync(ProcessOnce, cancellationToken);
    }

    private async Task ProcessOnce(IncomingMessagequeueItem item)
    {
        logger.LogTrace("Received message from queue to send out: {Message}", item);
        if (item.MessageType == MessageType.Chat)
        {
            await messageSender.SendMessage(item.Message);
        }
        else if (item.MessageType == MessageType.Whisper)
        {
            User? user = await userRepo.FindById(item.Target);
            if (user == null)
                logger.LogError(
                    "Cannot send out queued whisper message because User-ID '{UserId}' is unknown: {Message}",
                    item.Target, item);
            else
                await messageSender.SendWhisper(user, item.Message);
        }
        else
        {
            throw new ArgumentException("Unknown message type {}", item.MessageType.ToString());
        }
    }
}
