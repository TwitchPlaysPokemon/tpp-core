using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
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
        private readonly ISubscriptionProcessor _subscriptionProcessor;
        private readonly IMessageSender _responseSender;
        private readonly OverlayConnection _overlayConnection;

        public TwitchLibSubscriptionWatcher(
            ILoggerFactory loggerFactory, IUserRepo userRepo, TwitchClient twitchClient, IClock clock,
            ISubscriptionProcessor subscriptionProcessor, IMessageSender responseSender,
            OverlayConnection overlayConnection)
        {
            _logger = loggerFactory.CreateLogger<TwitchLibSubscriptionWatcher>();
            _userRepo = userRepo;
            _twitchClient = twitchClient;
            _clock = clock;
            _subscriptionProcessor = subscriptionProcessor;
            _responseSender = responseSender;
            _overlayConnection = overlayConnection;
            _twitchClient.OnNewSubscriber += OnNewSubscriber;
            _twitchClient.OnReSubscriber += OnReSubscriber;
            _twitchClient.OnGiftedSubscription += OnGiftedSubscription;
        }

        private static string BuildSubResponse(
            ISubscriptionProcessor.SubResult subResult, User? gifter, bool isAnonymous)
        {
            static string BuildOkMessage(ISubscriptionProcessor.SubResult.Ok ok, User? gifter, bool isAnonymous)
            {
                string message = "";
                if (ok.SubCountCorrected)
                    message +=
                        $"We detected that the amount of months subscribed ({ok.CumulativeMonths}) is lower than " +
                        "our system expected. This happened due to erroneously detected subscriptions in the past. " +
                        "Your account data has been adjusted accordingly, and you will receive your rewards normally. ";
                if (ok.NewLoyaltyLeague > ok.OldLoyaltyLeague)
                    message += $"You reached Loyalty League {ok.NewLoyaltyLeague}! ";
                if (ok.DeltaTokens > 0)
                    message += $"You gained T{ok.DeltaTokens} tokens! ";
                if (gifter != null && isAnonymous)
                    message += "An anonymous user gifted you a subscription!";
                else if (gifter != null && !isAnonymous)
                    message += $"{gifter.Name} gifted you a subscription!";
                else if (ok.CumulativeMonths > 1)
                    message += "Thank you for resubscribing!";
                else
                    message += "Thank you for subscribing!";
                return message;
            }

            return subResult switch
            {
                ISubscriptionProcessor.SubResult.Ok ok => BuildOkMessage(ok, gifter, isAnonymous),
                ISubscriptionProcessor.SubResult.SameMonth sameMonth =>
                    $"We detected that you've already announced your resub for month {sameMonth.Month}, " +
                    "and received the appropriate tokens. " +
                    "If you believe this is in error, please contact a moderator so this can be corrected.",
                _ => throw new ArgumentOutOfRangeException(nameof(subResult)),
            };
        }

        private async Task OnSubscribed(SubscriptionInfo e)
        {
            ISubscriptionProcessor.SubResult subResult = await _subscriptionProcessor.ProcessSubscription(e);
            string response = BuildSubResponse(subResult, null, false);
            await _responseSender.SendWhisper(e.Subscriber, response);

            await _overlayConnection.Send(new NewSubscriber
            {
                User = e.Subscriber,
                Emotes = e.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
                SubMessage = e.Message,
                ShareSub = true,
            }, CancellationToken.None);
        }

        private async Task OnSubscriptionGifted(SubscriptionGiftInfo e)
        {
            (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
                await _subscriptionProcessor.ProcessSubscriptionGift(e);

            string subResponse = BuildSubResponse(subResult, e.Gifter, e.IsAnonymous);
            await _responseSender.SendWhisper(e.SubscriptionInfo.Subscriber, subResponse);

            string subGiftResponse = subGiftResult switch
            {
                ISubscriptionProcessor.SubGiftResult.LinkedAccount =>
                    $"As you are linked to the account '{e.SubscriptionInfo.Subscriber.Name}' you have gifted to, " +
                    "you have not received a token bonus. " +
                    "The recipient account still gains the normal benefits however. Thanks for subscribing!",
                ISubscriptionProcessor.SubGiftResult.SameMonth { Month: var month } =>
                    $"We detected that this gift sub may have been a repeated message for month {month}, " +
                    "and you have already received the appropriate tokens. " +
                    "If you believe this is in error, please contact a moderator so this can be corrected.",
                ISubscriptionProcessor.SubGiftResult.Ok { GifterTokens: var tokens } =>
                    $"Thank you for your generosity! You received T{tokens} tokens for giving a gift " +
                    "subscription. The recipient has been notified and awarded their token benefits.",
                _ => throw new ArgumentOutOfRangeException(nameof(subGiftResult))
            };
            if (!e.IsAnonymous)
                await _responseSender.SendWhisper(e.Gifter,
                    subGiftResponse); // don't respond to the "AnAnonymousGifter" user

            await _overlayConnection.Send(new NewSubscriber
            {
                User = e.SubscriptionInfo.Subscriber,
                Emotes = e.SubscriptionInfo.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
                SubMessage = e.SubscriptionInfo.Message,
                ShareSub = false,
            }, CancellationToken.None);
        }

        private async Task OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.Subscriber,
                e.Subscriber.MsgParamSubPlan, e.Subscriber.MsgParamSubPlanName, e.Subscriber.ResubMessage,
                e.Subscriber.MsgParamStreakMonths, e.Subscriber.MsgParamCumulativeMonths);
            await OnSubscribed(subscriptionInfo);
        }

        private async Task OnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            SubscriptionInfo subscriptionInfo = await ParseSubscription(e.ReSubscriber,
                e.ReSubscriber.MsgParamSubPlan, e.ReSubscriber.MsgParamSubPlanName, e.ReSubscriber.ResubMessage,
                e.ReSubscriber.MsgParamStreakMonths, e.ReSubscriber.MsgParamCumulativeMonths);
            await OnSubscribed(subscriptionInfo);
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
            await OnSubscriptionGifted(subscriptionGiftInfo);
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
