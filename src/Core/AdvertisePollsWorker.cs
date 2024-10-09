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

namespace TPP.Core
{
    /// <summary>
    /// Advertises polls in chat on an interval.
    /// </summary>
    public sealed class AdvertisePollsWorker : IWithLifecycle
    {
        private readonly ILogger<AdvertisePollsWorker> _logger;
        private readonly Duration _interval;
        private readonly IPollRepo _pollRepo;
        private readonly IMessageSender _messageSender;

        public AdvertisePollsWorker(ILogger<AdvertisePollsWorker> logger, Duration interval, IPollRepo pollRepo,
            IMessageSender messageSender)
        {
            _logger = logger;
            _interval = interval;
            _pollRepo = pollRepo;
            _messageSender = messageSender;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            try { await Task.Delay(_interval.ToTimeSpan(), cancellationToken); }
            catch (OperationCanceledException) { return; }
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoLoop();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to advertise polls");
                }
                try { await Task.Delay(_interval.ToTimeSpan(), cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task DoLoop()
        {
            IImmutableList<Poll> polls = await _pollRepo.FindPolls(onlyActive: true);
            if (polls.Count == 0) return;
            if (polls.Count == 1)
            {
                await _messageSender.SendMessage(
                    "Please vote in the currently active poll: " +
                    PollCommands.FormatSinglePollAdvertisement(polls[0]));
            }
            else
            {
                await _messageSender.SendMessage(PollCommands.FormatPollsAdvertisement(polls));
            }
        }
    }
}
