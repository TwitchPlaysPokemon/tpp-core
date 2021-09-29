using Serilog.Events;

namespace TPP.Core.Configuration;

public sealed class DiscordLoggingConfig : ConfigBase
{
    public ulong WebhookId { get; init; } = 0L;
    public string WebhookToken { get; init; } = "";
    public LogEventLevel MinLogLevel { get; init; } = LogEventLevel.Warning;
}
