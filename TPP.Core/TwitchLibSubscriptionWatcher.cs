using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TPP.Core
{
    public sealed class TwitchLibSubscriptionWatcher : IDisposable
    {
        private readonly ILogger<TwitchLibSubscriptionWatcher> _logger;
        private readonly IUserRepo _userRepo;
        private readonly TwitchClient _twitchClient;
        private readonly IClock _clock;

        public event EventHandler<SubscriptionInfo>? Subscribed;
        public event EventHandler<SubscriptionGiftInfo>? SubscriptionGifted;

        public TwitchLibSubscriptionWatcher(
            ILogger<TwitchLibSubscriptionWatcher> logger, IUserRepo userRepo, TwitchClient twitchClient, IClock clock)
        {
            _logger = logger;
            _userRepo = userRepo;
            _twitchClient = twitchClient;
            _clock = clock;
            _twitchClient.OnNewSubscriber += OnNewSubscriber;
            _twitchClient.OnReSubscriber += OnReSubscriber;
            _twitchClient.OnGiftedSubscription += OnGiftedSubscription;
        }

        private async Task OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.Subscriber,
                e.Subscriber.MsgParamSubPlan, e.Subscriber.MsgParamSubPlanName, e.Subscriber.ResubMessage,
                e.Subscriber.MsgParamStreakMonths, e.Subscriber.MsgParamCumulativeMonths);
            Subscribed?.Invoke(this, subscriptionInfo);
        }

        private async Task OnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.ReSubscriber,
                e.ReSubscriber.MsgParamSubPlan, e.ReSubscriber.MsgParamSubPlanName, e.ReSubscriber.ResubMessage,
                e.ReSubscriber.MsgParamStreakMonths, e.ReSubscriber.MsgParamCumulativeMonths);
            Subscribed?.Invoke(this, subscriptionInfo);
        }

        private async Task OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
        {
            GiftedSubscription subscriptionMessage = e.GiftedSubscription;
            User gifter = await _userRepo.RecordUser(new UserInfo(
                subscriptionMessage.UserId,
                subscriptionMessage.DisplayName,
                subscriptionMessage.Login
            ));
            User user = await _userRepo.RecordUser(new UserInfo(
                subscriptionMessage.MsgParamRecipientId,
                subscriptionMessage.MsgParamRecipientDisplayName,
                subscriptionMessage.MsgParamRecipientUserName
            ));
            SubscriptionTier tier = subscriptionMessage.MsgParamSubPlan switch
            {
                SubscriptionPlan.Prime => SubscriptionTier.Prime,
                SubscriptionPlan.Tier1 => SubscriptionTier.Tier1,
                SubscriptionPlan.Tier2 => SubscriptionTier.Tier2,
                SubscriptionPlan.Tier3 => SubscriptionTier.Tier3,
                SubscriptionPlan.NotSet => throw new ArgumentOutOfRangeException(
                    $"subscription plan not set, plan name: {subscriptionMessage.MsgParamSubPlanName}"),
            };
            SubscriptionInfo subscriptionInfo = new(
                user, int.Parse(subscriptionMessage.MsgParamMonths), StreakMonths: 1, tier,
                subscriptionMessage.MsgParamSubPlanName, _clock.GetCurrentInstant(),
                Message: null, ParseEmotes(e.GiftedSubscription.Emotes));
            int numGiftedMonths = e.GiftedSubscription.MsgParamMultiMonthGiftDuration;
            SubscriptionGiftInfo subscriptionGiftInfo = new(subscriptionInfo, gifter, numGiftedMonths,
                subscriptionMessage.IsAnonymous);
            SubscriptionGifted?.Invoke(this, subscriptionGiftInfo);
        }

        private async Task<SubscriptionInfo> ParseSubscription(
            UserNoticeBase subscriptionMessage,
            SubscriptionPlan planTier, string planName, string? resubMessage,
            int streakMonths, int cumulativeMonths)
        {
            User user = await _userRepo.RecordUser(new UserInfo(
                subscriptionMessage.UserId,
                subscriptionMessage.DisplayName,
                subscriptionMessage.Login));
            SubscriptionTier tier = planTier switch
            {
                SubscriptionPlan.Prime => SubscriptionTier.Prime,
                SubscriptionPlan.Tier1 => SubscriptionTier.Tier1,
                SubscriptionPlan.Tier2 => SubscriptionTier.Tier2,
                SubscriptionPlan.Tier3 => SubscriptionTier.Tier3,
                SubscriptionPlan.NotSet => throw new ArgumentOutOfRangeException(
                    $"subscription plan not set, plan name: {planName}"),
            };
            string? message = string.IsNullOrWhiteSpace(resubMessage) ? null : resubMessage;
            return new SubscriptionInfo(
                user, cumulativeMonths, streakMonths, tier,
                planName, _clock.GetCurrentInstant(), message,
                ParseEmotes(subscriptionMessage.Emotes));
        }

        private static IImmutableList<EmoteOccurrence> ParseEmotes(string emoteString) =>
            new EmoteSet(emoteString, string.Empty).Emotes
                .Select(e => new EmoteOccurrence(e.Id, e.Name, e.StartIndex, e.EndIndex))
                .ToImmutableList();

        public void Dispose()
        {
            _twitchClient.OnNewSubscriber -= OnNewSubscriber;
            _twitchClient.OnReSubscriber -= OnReSubscriber;
            _twitchClient.OnGiftedSubscription -= OnGiftedSubscription;
        }
    }
}
