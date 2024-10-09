using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;

namespace TPP.Core.Chat;

/// <summary>
/// If expiring tokens are in use (some old tokens may not expire, but newer ones generally do: https://dev.twitch.tv/docs/authentication/refresh-tokens/),
/// those need to be refreshed using a refresh token. This class employs a quick and dirty refresh mechanism.
/// </summary>
public class TwitchApiProvider
{
    private static readonly Duration RefreshEarlier = Duration.FromSeconds(10);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TwitchApiProvider> _logger;
    private readonly IClock _clock;
    private readonly string? _infiniteAccessToken;
    private readonly string? _refreshToken;
    private readonly string _appClientId;
    private readonly string _appClientSecret;
    private readonly TwitchLib.Api.TwitchAPI _authlessApi;
    private TwitchLib.Api.TwitchAPI? _api = null;
    private Instant _apiValidUntil = Instant.MinValue;

    public TwitchApiProvider(
        ILoggerFactory loggerFactory,
        IClock clock,
        string? infiniteAccessToken,
        string? refreshToken,
        string appClientId,
        string appClientSecret)
    {
        if (infiniteAccessToken == null && refreshToken == null)
            throw new ArgumentException("Cannot omit both the access token and the refresh token in the twitch config");
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TwitchApiProvider>();
        _clock = clock;
        _infiniteAccessToken = infiniteAccessToken;
        _refreshToken = refreshToken;
        _authlessApi = new TwitchLib.Api.TwitchAPI();
        _appClientId = appClientId;
        _appClientSecret = appClientSecret;
    }

    private async Task<TwitchLib.Api.TwitchAPI> GetInternal()
    {
        Instant now = _clock.GetCurrentInstant();
        if (_api != null && _apiValidUntil >= now - RefreshEarlier)
        {
            return _api;
        }
        if (_api == null && _infiniteAccessToken != null)
        {
            _logger.LogDebug("Twitch-API access_token configured, assuming token with infinite validity, using it");
            _api = new TwitchLib.Api.TwitchAPI(_loggerFactory, settings: new ApiSettings
            {
                ClientId = _appClientId,
                AccessToken = _infiniteAccessToken
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
        _api = new TwitchLib.Api.TwitchAPI(_loggerFactory, settings: new ApiSettings
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

    public async Task<TwitchLib.Api.TwitchAPI> Get()
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

    public async Task Invalidate()
    {
        await _semaphore.WaitAsync();
        try
        {
            _apiValidUntil = Instant.MinValue;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
