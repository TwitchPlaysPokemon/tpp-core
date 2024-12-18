using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Streamlabs;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core;

public sealed class DonationsWorker(
    ILoggerFactory loggerFactory,
    TimeSpan pollingInterval,
    StreamlabsClient streamlabsClient,
    IDonationRepo donationRepo,
    DonationHandler donationHandler
) : IWithLifecycle
{
    private readonly ILogger<ChattersWorker> _logger = loggerFactory.CreateLogger<ChattersWorker>();

    public async Task Start(CancellationToken cancellationToken)
    {
        try { await Task.Delay(pollingInterval, cancellationToken); }
        catch (OperationCanceledException) { return; }
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Donation? mostRecentDonation = await donationRepo.GetMostRecentDonation();
                _logger.LogDebug("Polling for new donations... most recent one is {Donation}", mostRecentDonation);
                List<StreamlabsClient.Donation> donations =
                    await streamlabsClient.GetDonations(after: mostRecentDonation?.DonationId, currency: "USD");
                _logger.LogDebug("Received new donations: {Donations}", string.Join(", ", donations));
                foreach (var donation in donations.OrderBy(d => d.CreatedAt)) // process in chronological order
                    await donationHandler.Process(DonationHandler.NewDonation.FromStreamlabs(donation));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed polling for new donations");
            }

            try { await Task.Delay(pollingInterval, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
