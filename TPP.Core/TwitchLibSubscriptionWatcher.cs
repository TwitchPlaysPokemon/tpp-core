using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TPP.Core
{
    public sealed class TwitchLibSubscriptionWatcher : IDisposable
    {
        private readonly IUserRepo _userRepo;
        private readonly TwitchClient _twitchClient;
        private readonly IClock _clock;

        public event EventHandler<SubscriptionInfo>? Subscribed;
        public event EventHandler<SubscriptionGiftInfo>? SubscriptionGifted;

        public TwitchLibSubscriptionWatcher(
            IUserRepo userRepo, TwitchClient twitchClient, IClock clock)
        {
            _userRepo = userRepo;
            _twitchClient = twitchClient;
            _clock = clock;
            _twitchClient.OnNewSubscriber += OnNewSubscriber;
            _twitchClient.OnReSubscriber += OnReSubscriber;
            _twitchClient.OnGiftedSubscription += OnGiftedSubscription;
        }

        private async void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.Subscriber);
            Subscribed?.Invoke(this, subscriptionInfo);
        }

        private async void OnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.ReSubscriber);
            Subscribed?.Invoke(this, subscriptionInfo);
        }

        private async void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
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
                _ => throw new ArgumentOutOfRangeException(
                    $"unknown subscription plan '{subscriptionMessage.MsgParamSubPlan}'")
            };
            SubscriptionInfo subscriptionInfo = new(
                user, int.Parse(subscriptionMessage.MsgParamMonths), StreakMonths: 1, tier,
                subscriptionMessage.MsgParamSubPlanName, _clock.GetCurrentInstant(),
                Message: null, ParseEmotes(e.GiftedSubscription.Emotes));
            SubscriptionGiftInfo subscriptionGiftInfo = new(subscriptionInfo, gifter, subscriptionMessage.IsAnonymous);
            SubscriptionGifted?.Invoke(this, subscriptionGiftInfo);
        }

        private async Task<SubscriptionInfo> ParseSubscription(SubscriberBase subscriptionMessage)
        {
            User user = await _userRepo.RecordUser(new UserInfo(
                subscriptionMessage.UserId,
                subscriptionMessage.DisplayName,
                subscriptionMessage.Login
            ));
            SubscriptionTier tier = subscriptionMessage.SubscriptionPlan switch
            {
                SubscriptionPlan.Prime => SubscriptionTier.Prime,
                SubscriptionPlan.Tier1 => SubscriptionTier.Tier1,
                SubscriptionPlan.Tier2 => SubscriptionTier.Tier2,
                SubscriptionPlan.Tier3 => SubscriptionTier.Tier3,
                SubscriptionPlan.NotSet => throw new ArgumentOutOfRangeException(
                    $"subscription plan not set, plan name: {subscriptionMessage.SubscriptionPlanName}"),
                _ => throw new ArgumentOutOfRangeException(
                    $"unknown subscription plan '{subscriptionMessage.SubscriptionPlan}'")
            };
            string? message = string.IsNullOrWhiteSpace(subscriptionMessage.ResubMessage)
                ? null
                : subscriptionMessage.ResubMessage;
            if (!int.TryParse(subscriptionMessage.MsgParamStreakMonths, out int streakMonths)) streakMonths = 1;
            return new SubscriptionInfo(
                user, int.Parse(subscriptionMessage.MsgParamCumulativeMonths), streakMonths, tier,
                subscriptionMessage.SubscriptionPlanName, _clock.GetCurrentInstant(), message,
                ParseEmotes(subscriptionMessage.EmoteSet));
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
