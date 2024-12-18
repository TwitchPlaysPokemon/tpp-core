using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Core.Streamlabs;
using TPP.Model;
using TPP.Persistence;
using static TPP.Common.Utils.StringExtensions;

namespace TPP.Core;

public class DonationHandler(
    ILogger<DonationHandler> logger,
    IDonationRepo donationRepo,
    IUserRepo userRepo,
    IBank<User> tokensBank,
    IMessageSender messageSender,
    OverlayConnection overlayConnection,
    IChattersSnapshotsRepo chattersSnapshotsRepo,
    int donorBadgeCents)
{
    public record NewDonation(
        long Id,
        Instant CreatedAt,
        string Username,
        decimal Amount,
        string Currency,
        string? Message)
    {
        public static NewDonation FromStreamlabs(StreamlabsClient.Donation donation) => new(
            Id: donation.DonationId,
            CreatedAt: donation.CreatedAt,
            Username: donation.Name,
            Amount: donation.Amount,
            Currency: donation.Currency,
            Message: donation.Message
        );
    }

    public async Task Process(NewDonation donation)
    {
        if (await donationRepo.FindDonation(donation.Id) is { } existingDonation)
        {
            logger.LogDebug("Skipping donation because it already exists in the database. " +
                            "Donation: {Donation}, existing donation: {DbDonation}", donation, existingDonation);
            return;
        }
        // We only support processing USD.
        // Streamlabs API uses apostrophes to indicate that it may have converted the amount to USD from its original currency.
        if (donation.Currency is not ("'USD'" or "USD"))
        {
            logger.LogError("Skipping donation because its amount must be in USD. Donation: {Donation}", donation);
            return;
        }
        int cents = (int)Math.Round(donation.Amount * 100, 0);

        User? donor = await userRepo.FindBySimpleName(donation.Username.ToLower());
        if (donor is null)
            // Warn, but store it in the DB anyway
            logger.LogWarning("Could not find a user for donor: {Donor}", donation.Username);

        logger.LogInformation("New donation: {Donation}", donation);

        await donationRepo.InsertDonation(
            donationId: donation.Id,
            createdAt: donation.CreatedAt,
            userName: donation.Username,
            userId: donor?.Id,
            cents: cents,
            message: donation.Message
        );

        var recordBreaks = await GetRecordBreaks(donation.Id, donation.CreatedAt);
        DonationTokens tokens = GetDonationTokens(cents, recordBreaks);
        if (donor != null)
        {
            await UpdateHasDonationBadge(donor);
            await GivenTokensToDonorAndNotifyThem(donor, donation.Id, tokens);
        }
        await RandomlyDistributeTokens(donation.CreatedAt, donation.Id, donation.Username, tokens.Total());
        await overlayConnection.Send(new NewDonationEvent
        {
            // We used to look up emotes using the internal Emote Service, but this small feature (emotes in donations)
            // was the only thing remaining using the Emote Service, so it's not worth it.
            Emotes = [],
            RecordDonations = new Dictionary<int, List<string>>
            {
                [cents] = recordBreaks.Select(recordBreak => recordBreak.Name).ToList()
            },
            Donation = new NewDonationEvent.DonationInfo
            {
                Username = donation.Username,
                Cents = cents,
                Message = donation.Message
            }
        }, CancellationToken.None);
    }

    private async Task UpdateHasDonationBadge(User user)
    {
        HashSet<string> userIds = [user.Id];
        bool hasCentsRequired = (await donationRepo.GetCentsPerUser(donorBadgeCents, userIds)).ContainsKey(user.Id);
        logger.LogDebug("User {Username} should have donor badge now? {HasDonorBadge}", user.Name, hasCentsRequired);
        await userRepo.SetHasDonorBadge(user, hasCentsRequired);
    }

    record DonationTokens(int Base, int Bonus)
    {
        public int Total() => Base + Bonus;
    }

    /// Assumes the donation has already been persisted.
    private async Task<ImmutableSortedSet<DonationRecordBreakType>> GetRecordBreaks(long donationId, Instant createdAt)
    {
        return (await donationRepo.GetRecordDonations(createdAt))
            .Where(kvp => kvp.Value.DonationId == donationId)
            .Select(kvp => kvp.Key)
            .ToImmutableSortedSet();
    }

    /// Calculated a donation's reward tokens, which consists of some base tokens per cents,
    /// plus bonus tokens obtained from donation record breaks.
    private DonationTokens GetDonationTokens(int cents, ISet<DonationRecordBreakType> recordBreaks)
    {
        const int centsPerToken = 50;

        int baseTokens = cents / centsPerToken;
        int bonusTokens = recordBreaks.Sum(recordBreakType => recordBreakType.TokenWinning);

        return new DonationTokens(baseTokens, bonusTokens);
    }

    private async Task GivenTokensToDonorAndNotifyThem(User user, long donationId, DonationTokens tokens)
    {
        var additionalData = new Dictionary<string, object?>
        {
            ["donation"] = donationId,
            ["donation_base_tokens"] = tokens.Base,
            ["donation_bonus_tokens"] = tokens.Bonus,
        };
        var transaction = new Transaction<User>(user, tokens.Total(), TransactionType.DonationTokens, additionalData);
        await tokensBank.PerformTransaction(transaction);

        string message = tokens.Bonus > 0
            ? $"You got T{tokens.Base} + T{tokens.Bonus} from record breaks for your donation!"
            : $"You got T{tokens.Base} for your donation!";
        await messageSender.SendWhisper(user, message);
    }

    private async Task RandomlyDistributeTokens(Instant createdAt, long donationId, string donorName, int tokens)
    {
        ChattersSnapshot? snapshot = await chattersSnapshotsRepo.GetRecentChattersSnapshot(
            from: createdAt.Minus(Duration.FromMinutes(10)),
            to: createdAt);
        IReadOnlyList<string> candidateIds = snapshot?.ChatterIds ?? [];
        logger.LogDebug("Token distribution candidates before eligibility filter: {Candidates}", candidateIds);
        List<User> eligibleUsers = await userRepo.FindByIdsEligibleForHandouts(candidateIds);
        if (eligibleUsers.Count == 0)
        {
            logger.LogWarning("Aborting distribution of {NumTokens} random tokens due to lack of candidates", tokens);
            return;
        }

        Random rng = new();
        Dictionary<User, int> winners = Enumerable
            .Range(0, tokens)
            .Select(_ => eligibleUsers[rng.Next(eligibleUsers.Count)])
            .GroupBy(user => user)
            .ToDictionary(grp => grp.Key, grp => grp.Count());
        logger.LogInformation("Some users won tokens from a random donation distribution: {UsersToTokens}",
            string.Join(", ", winners.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        foreach ((User recipient, int winnerTokens) in winners)
            await GivenTokensToRandomRecipientAndNotifyThem(recipient, donorName, donationId, winnerTokens);
    }

    private async Task GivenTokensToRandomRecipientAndNotifyThem(
        User recipient, string donorName, long donationId, int tokens)
    {
        var transaction = new Transaction<User>(recipient, tokens, TransactionType.DonationRandomlyDistributedTokens,
            new Dictionary<string, object?> { ["donation"] = donationId });
        await tokensBank.PerformTransaction(transaction);
        await messageSender.SendWhisper(recipient, $"You won T{tokens} from {donorName.Genitive()} donation!");
    }
}
