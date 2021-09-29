using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using TPP.Core.Chat;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core;

/// <summary>
/// Advertises polls in chat on an interval.
/// </summary>
public sealed class AdvertisePollsWorker : IDisposable
{
    private readonly Duration _interval;
    private readonly IPollRepo _pollRepo;
    private readonly IMessageSender _messageSender;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runTask = null;

    public AdvertisePollsWorker(Duration interval, IPollRepo pollRepo, IMessageSender messageSender)
    {
        _interval = interval;
        _pollRepo = pollRepo;
        _messageSender = messageSender;
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
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(_interval.ToTimeSpan(), _cancellationTokenSource.Token);
            IImmutableList<Poll> polls = await _pollRepo.FindPolls(onlyActive: true);
            if (polls.Count == 0) continue;
            if (polls.Count == 1)
            {
                await _messageSender.SendMessage("Please vote in the currently active poll: " +
                                                 PollCommands.FormatSinglePollAdvertisement(polls[0]));
            }
            else
            {
                await _messageSender.SendMessage(PollCommands.FormatPollsAdvertisement(polls));
            }
        }
    }

    public void Dispose()
    {
        if (_runTask != null) Stop().Wait();
        _cancellationTokenSource.Dispose();
        _runTask?.Dispose();
    }
}
