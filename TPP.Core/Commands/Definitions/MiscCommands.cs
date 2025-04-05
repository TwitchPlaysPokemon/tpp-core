using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.Extensions;
using TPP.Common;
using TPP.Core.Chat;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace TPP.Core.Commands.Definitions;

class UptimeData
{
    public Instant LastUpdatedAt = Instant.MinValue;
    public Instant? StartedAt = null;
}

/// miscellaneous commands that don't justify their own file
public class MiscCommands : ICommandCollection
{
    private static readonly Duration MaxUptimeUpdateAge = Duration.FromMinutes(1);

    // assuming this class gets loaded at startup, which it does
    private readonly Instant _upSince;
    private readonly IClock _clock;
    private readonly IDictionary<IChat, UptimeData> _uptimeDict = new Dictionary<IChat, UptimeData>();

    public MiscCommands(IClock clock)
    {
        _clock = clock;
        _upSince = _clock.GetCurrentInstant();
    }

    private async Task<CommandResult> Uptime(CommandContext ctx)
    {
        Instant now = _clock.GetCurrentInstant();

        IChat? chat = ctx.Source;
        if (chat is not TwitchChat twitchChat)
            return new CommandResult { Response = "The uptime command is not supported in this chat." };
        if (!_uptimeDict.ContainsKey(chat))
            _uptimeDict[chat] = new UptimeData();
        UptimeData uptimeData = _uptimeDict[chat];

        if (uptimeData.LastUpdatedAt < now - MaxUptimeUpdateAge)
        {
            GetStreamsResponse apiResponse =
                await twitchChat.TwitchApi.GetStreamsAsync(userIds: new List<string> { twitchChat.ChannelId });
            Stream? stream = apiResponse.Streams.FirstOrDefault(); // may be null if offline (or wrong channel id)
            uptimeData.StartedAt = stream?.StartedAt.ToInstant();
            uptimeData.LastUpdatedAt = now;
        }

        string streamUptimeResponse;
        if (uptimeData.StartedAt == null)
        {
            streamUptimeResponse = "The stream is currently offline, or uptime information is unavailable.";
        }
        else
        {
            Duration streamUptime = now - uptimeData.StartedAt.Value;
            string streamUptimeFormatted = streamUptime.ToTimeSpan().ToHumanReadable(FormatPrecision.Adaptive);
            streamUptimeResponse =
                $"The stream has been online for {streamUptimeFormatted} (since {uptimeData.StartedAt}).";
        }
        Duration coreUptime = now - _upSince;
        string coreUptimeFormatted = coreUptime.ToTimeSpan().ToHumanReadable(FormatPrecision.Adaptive);
        string coreUptimeResponse = $"The TPP core has been running for {coreUptimeFormatted} (since {_upSince}).";
        return new CommandResult { Response = streamUptimeResponse + " " + coreUptimeResponse };
    }

    public IEnumerable<Command> Commands => new[]
    {
        new Command("unsupported", _ => Task.FromResult(new CommandResult()))
        {
            Description = "Prevents stupid loops when a core messages another core",
            Aliases = new[] { "unknown" }
        },

        new Command("uptime", Uptime) { Description = "Shows how long the stream has been up." }
    };
}
