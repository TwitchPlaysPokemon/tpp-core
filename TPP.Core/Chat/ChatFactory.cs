using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat;

public class ChatFactory(
    ILoggerFactory loggerFactory,
    IClock clock,
    IUserRepo userRepo,
    ICoStreamChannelsRepo coStreamChannelsRepo,
    IBank<User> tokenBank,
    ISubscriptionLogRepo subscriptionLogRepo,
    ILinkedAccountRepo linkedAccountRepo,
    OverlayConnection overlayConnection)
{
    public IChat Create(ConnectionConfig config) =>
        config switch
        {
            ConnectionConfig.Console cfg => new ConsoleChat(config.Name, loggerFactory, cfg, userRepo),
            ConnectionConfig.Twitch cfg => new TwitchChat(config.Name, loggerFactory, clock,
                cfg, userRepo, coStreamChannelsRepo,
                new SubscriptionProcessor(
                    loggerFactory.CreateLogger<SubscriptionProcessor>(),
                    tokenBank, userRepo, subscriptionLogRepo, linkedAccountRepo),
                overlayConnection),
            ConnectionConfig.Simulation cfg => new SimulationChat(config.Name, loggerFactory, cfg, userRepo),
            _ => throw new ArgumentOutOfRangeException(nameof(config), "unknown chat connector type")
        };
}
