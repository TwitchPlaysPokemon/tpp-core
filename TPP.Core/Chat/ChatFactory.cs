using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Persistence.Repos;

namespace TPP.Core.Chat
{
    public class ChatFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IClock _clock;
        private readonly IUserRepo _userRepo;

        public ChatFactory(ILoggerFactory loggerFactory, IClock clock, IUserRepo userRepo)
        {
            _loggerFactory = loggerFactory;
            _clock = clock;
            _userRepo = userRepo;
        }

        public IChat Create(ConnectionConfig config) =>
            config switch
            {
                ConnectionConfig.Console cfg => new ConsoleChat(config.Name, _loggerFactory, cfg, _userRepo),
                ConnectionConfig.Twitch cfg => new TwitchChat(config.Name, _loggerFactory, _clock, cfg, _userRepo),
                _ => throw new ArgumentOutOfRangeException(nameof(config), "unknown chat connector type")
            };
    }
}
