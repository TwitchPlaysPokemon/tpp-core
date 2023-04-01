using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;

namespace TPP.Core.Chat;

/// <summary>
/// Currently, access tokens don't seem to expire, but Twitch says they do: https://dev.twitch.tv/docs/authentication/refresh-tokens/
/// Just to be sure we don't just die once they actually do expire, let's employ a quick and dirty refresh mechanism.
/// </summary>
public class TwitchApiProvider
{
    private static readonly Duration RefreshEarlier = Duration.FromSeconds(10);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TwitchApiProvider> _logger;
    private readonly IClock _clock;
    private readonly string? _accessToken;
    private readonly string? _refreshToken;
    private readonly string _appClientId;
    private readonly string _appClientSecret;
    private readonly TwitchAPI _authlessApi;
    private TwitchAPI? _api = null;
    private Instant _apiValidUntil = Instant.MinValue;

    public TwitchApiProvider(
        ILoggerFactory loggerFactory,
        IClock clock,
        string? accessToken,
        string? refreshToken,
        string appClientId,
        string appClientSecret)
    {
        if (accessToken == null && refreshToken == null)
            throw new ArgumentException("Cannot omit both the access token and the refresh token in the twitch config");
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TwitchApiProvider>();
        _clock = clock;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _authlessApi = new TwitchAPI();
        _appClientId = appClientId;
        _appClientSecret = appClientSecret;
    }

    private async Task<TwitchAPI> GetInternal()
    {
        Instant now = _clock.GetCurrentInstant();
        if (_api != null && _apiValidUntil >= now - RefreshEarlier)
        {
            return _api;
        }
        if (_api == null && _accessToken != null)
        {
            _logger.LogDebug("Twitch-API access_token configured, assuming token with infinite validity, using it");
            _api = new TwitchAPI(_loggerFactory, settings: new ApiSettings
            {
                ClientId = _appClientId,
                AccessToken = _accessToken
            });
            _apiValidUntil = Instant.MaxValue;
            return _api;
        }
        _logger.LogDebug("Twitch-API access_token expired or not initialized, refreshing API...");
        RefreshResponse result = await _authlessApi.Auth.RefreshAuthTokenAsync(
            refreshToken: _refreshToken,
            clientSecret: _appClientSecret,
            clientId: _appClientId
        ) ?? throw new ArgumentNullException(null, "The refresh auth token result cannot be null");
        _api = new TwitchAPI(_loggerFactory, settings: new ApiSettings
        {
            ClientId = _appClientId,
            AccessToken = result.AccessToken
        });
        if (result.ExpiresIn <= 0)
        {
            _apiValidUntil = Instant.MaxValue;
            _logger.LogDebug("Received non-expiring token, setting expiry date to {ExpiresAt}", _apiValidUntil);
        }
        else
        {
            _apiValidUntil = now.Plus(Duration.FromSeconds(result.ExpiresIn));
            _logger.LogDebug("New access token expires in {ExpiresIn}s, at {ExpiresAt}", result.ExpiresIn,
                _apiValidUntil);
        }
        return _api;
    }

    public async Task<TwitchAPI> Get()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await GetInternal();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
