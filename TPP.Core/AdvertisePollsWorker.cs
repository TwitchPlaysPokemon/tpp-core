using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core;

/// <summary>
/// Advertises polls in chat on an interval.
/// </summary>
public sealed class AdvertisePollsWorker(
    ILogger<AdvertisePollsWorker> logger,
    Duration interval,
    IPollRepo pollRepo,
    IMessageSender messageSender)
    : IWithLifecycle
{
    public async Task Start(CancellationToken cancellationToken)
    {
        do
        {
            await Task.Delay(interval.ToTimeSpan(), cancellationToken);
            try
            {
                await DoLoop();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to advertise polls");
            }
        } while (!cancellationToken.IsCancellationRequested);
    }

    private async Task DoLoop()
    {
        IImmutableList<Poll> polls = await pollRepo.FindPolls(onlyActive: true);
        if (polls.Count == 0) return;
        if (polls.Count == 1)
        {
            await messageSender.SendMessage(
                "Please vote in the currently active poll: " +
                PollCommands.FormatSinglePollAdvertisement(polls[0]));
        }
        else
        {
            await messageSender.SendMessage(PollCommands.FormatPollsAdvertisement(polls));
        }
    }
}
