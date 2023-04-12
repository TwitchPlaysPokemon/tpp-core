using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace TPP.Core.Utils;

public static class LoggingExtensions
{
    /// Inspired by <see cref="Serilog.Extensions.Logging.File.Conversions.MicrosoftToSerilogLevel"/>
    public static LogEventLevel ToSerilogLogLevel(this LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Verbose
        };
}
