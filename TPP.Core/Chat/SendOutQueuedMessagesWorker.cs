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
public sealed class SendOutQueuedMessagesWorker : IDisposable
{
    private readonly ILogger<SendOutQueuedMessagesWorker> _logger;
    private readonly IIncomingMessagequeueRepo _incomingMessagequeueRepo;
    private readonly IUserRepo _userRepo;
    private readonly IMessageSender _messageSender;
    private readonly IClock _clock;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runTask = null;

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
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        if (_runTask != null) throw new InvalidOperationException("the worker is already running!");
        _runTask = Run();
    }

    private async Task Stop()
    {
        if (_runTask == null) throw new InvalidOperationException("the worker is not running!");
        _cancellationTokenSource.Cancel();
        try
        {
            await _runTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task Run()
    {
        Instant olderThan = _clock.GetCurrentInstant() - Duration.FromMinutes(5);
        await _incomingMessagequeueRepo.Prune(olderThan);
        await _incomingMessagequeueRepo.ForEachAsync(async item =>
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
        }, _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        if (_runTask != null) Stop().Wait();
        _cancellationTokenSource.Dispose();
        _runTask?.Dispose();
    }
}
