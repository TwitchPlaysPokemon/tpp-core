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
    private readonly ILogger<DonationsWorker> _logger = loggerFactory.CreateLogger<DonationsWorker>();

    public async Task Start(CancellationToken cancellationToken)
    {
        try { await Task.Delay(pollingInterval, cancellationToken); }
        catch (OperationCanceledException) { return; }
        int failureCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Donation? mostRecentDonation = await donationRepo.GetMostRecentDonation();
                _logger.LogDebug("Polling for new donations... most recent one is {DonationId}",
                    mostRecentDonation?.DonationId);
                List<StreamlabsClient.Donation> donations =
                    await streamlabsClient.GetDonations(after: mostRecentDonation?.DonationId, currency: "USD");
                _logger.LogDebug("Received new donations: {Donations}", string.Join(", ", donations));
                foreach (var donation in donations.OrderBy(d => d.CreatedAt)) // process in chronological order
                    await donationHandler.Process(DonationHandler.NewDonation.FromStreamlabs(donation));
                failureCount = 0;
            }
            catch (Exception e)
            {
                failureCount += 1;
                // We don't care about transient failures. Until it keeps failing, stick to debug logging.
                _logger.LogDebug(e, "Failed polling for new donations (failure count {FailureCount})", failureCount);
                if (failureCount >= 3)
                    _logger.LogError(e, "Failed polling for new donations");
            }

            try { await Task.Delay(pollingInterval, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
