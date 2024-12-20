using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Configuration;
using TPP.Model;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace TPP.Core;

public class TwitchEmotesLookup(
    ILoggerFactory loggerFactory,
    TwitchApi twitchApi,
    ConnectionConfig.Twitch chatConfig
) : IWithLifecycle
{
    private readonly ILogger<TwitchEmotesLookup> _logger = loggerFactory.CreateLogger<TwitchEmotesLookup>();
    private readonly TimeSpan _refreshInterval = chatConfig.GetEmotesInterval!.Value.ToTimeSpan();
    private Dictionary<string, EmoteInfo> _knownEmotesByCode = [];
    private Regex _emoteCodesRegex = new("Kappa");

    public List<EmoteInfo> FindEmotesInText(string text)
    {
        MatchCollection matches = _emoteCodesRegex.Matches(text);
        return matches.Select(match => _knownEmotesByCode[match.Value]).ToList();
    }

    private async Task RenewEmotes()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        GlobalEmote[] globalEmotes = (await twitchApi.GetGlobalEmotes()).GlobalEmotes;
        ChannelEmote[] channelEmotes = (await twitchApi.GetChannelEmotes(chatConfig.ChannelId)).ChannelEmotes;
        stopwatch.Stop();
        _logger.LogDebug("Retrieved {NumGlobalEmotes} global and {NumChannelEmotes} channel emotes in {ElapsedMs}ms",
            globalEmotes.Length, channelEmotes.Length, stopwatch.ElapsedMilliseconds);

        IEnumerable<EmoteInfo> globalEmoteInfos = globalEmotes.Select(e => new EmoteInfo
        {
            Code = e.Name, Id = e.Id,
            X1 = e.Images.Url1X, X2 = e.Images.Url2X, X3 = e.Images.Url4X
        });
        IEnumerable<EmoteInfo> channelEmoteInfos = channelEmotes.Select(e => new EmoteInfo
        {
            Code = e.Name, Id = e.Id,
            X1 = e.Images.Url1X, X2 = e.Images.Url2X, X3 = e.Images.Url4X
        });

        // Don't use ToDictionary, which forbids duplicate keys, because emote codes are not unique.
        // E.g. ':D' can either be ID 3 or 555555560, but they look identical anyway.
        Dictionary<string, EmoteInfo> knownEmotesByCode = [];
        foreach (EmoteInfo emoteInfo in globalEmoteInfos)
            knownEmotesByCode[emoteInfo.Code] = emoteInfo;
        foreach (EmoteInfo emoteInfo in channelEmoteInfos)
            knownEmotesByCode[emoteInfo.Code] = emoteInfo;
        _knownEmotesByCode = knownEmotesByCode;
        _emoteCodesRegex = new Regex(string.Join('|', _knownEmotesByCode.Keys.Select(Regex.Escape)));
        _logger.LogDebug("New emotes list: {Emotes}", string.Join(", ", _knownEmotesByCode.Keys));
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        // don't wait at startup, refresh right away
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RenewEmotes();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed renewing emotes");
            }

            try { await Task.Delay(_refreshInterval, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
