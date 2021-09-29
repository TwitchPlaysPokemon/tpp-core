using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat;

public class ChatFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IClock _clock;
    private readonly IUserRepo _userRepo;
    private readonly IBank<User> _tokenBank;
    private readonly ISubscriptionLogRepo _subscriptionLogRepo;
    private readonly ILinkedAccountRepo _linkedAccountRepo;
    private readonly OverlayConnection _overlayConnection;

    public ChatFactory(
        ILoggerFactory loggerFactory, IClock clock, IUserRepo userRepo, IBank<User> tokenBank,
        ISubscriptionLogRepo subscriptionLogRepo, ILinkedAccountRepo linkedAccountRepo,
        OverlayConnection overlayConnection)
    {
        _loggerFactory = loggerFactory;
        _clock = clock;
        _userRepo = userRepo;
        _tokenBank = tokenBank;
        _subscriptionLogRepo = subscriptionLogRepo;
        _linkedAccountRepo = linkedAccountRepo;
        _overlayConnection = overlayConnection;
    }

    public IChat Create(ConnectionConfig config) =>
        config switch
        {
            ConnectionConfig.Console cfg => new ConsoleChat(config.Name, _loggerFactory, cfg, _userRepo),
            ConnectionConfig.Twitch cfg => new TwitchChat(config.Name, _loggerFactory, _clock, cfg, _userRepo,
                new SubscriptionProcessor(
                    _loggerFactory.CreateLogger<SubscriptionProcessor>(),
                    _tokenBank, _userRepo, _subscriptionLogRepo, _linkedAccountRepo),
                _overlayConnection),
            ConnectionConfig.Simulation cfg => new SimulationChat(config.Name, _loggerFactory, cfg, _userRepo),
            _ => throw new ArgumentOutOfRangeException(nameof(config), "unknown chat connector type")
        };
}
