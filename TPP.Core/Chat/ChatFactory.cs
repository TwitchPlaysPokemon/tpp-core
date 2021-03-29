using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Chat
{
    public class ChatFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IClock _clock;
        private readonly IUserRepo _userRepo;
        private readonly IBank<User> _tokenBank;
        private readonly ISubscriptionLogRepo _subscriptionLogRepo;
        private readonly ILinkedAccountRepo _linkedAccountRepo;

        public ChatFactory(
            ILoggerFactory loggerFactory, IClock clock, IUserRepo userRepo, IBank<User> tokenBank,
            ISubscriptionLogRepo subscriptionLogRepo, ILinkedAccountRepo linkedAccountRepo)
        {
            _loggerFactory = loggerFactory;
            _clock = clock;
            _userRepo = userRepo;
            _tokenBank = tokenBank;
            _subscriptionLogRepo = subscriptionLogRepo;
            _linkedAccountRepo = linkedAccountRepo;
        }

        public IChat Create(ConnectionConfig config) =>
            config switch
            {
                ConnectionConfig.Console cfg => new ConsoleChat(config.Name, _loggerFactory, cfg, _userRepo),
                ConnectionConfig.Twitch cfg => new TwitchChat(config.Name, _loggerFactory, _clock, cfg, _userRepo,
                    new SubscriptionProcessor(_tokenBank, _userRepo, _subscriptionLogRepo, _linkedAccountRepo)),
                _ => throw new ArgumentOutOfRangeException(nameof(config), "unknown chat connector type")
            };
    }
}
