using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;

namespace TPP.Core.Commands.Definitions
{
    /// miscellaneous commands that don't justify their own file
    public class MiscCommands : ICommandCollection
    {
        // assuming this class gets loaded at startup, which it does
        private readonly Instant _upSince = SystemClock.Instance.GetCurrentInstant();

        public IEnumerable<Command> Commands => new[]
        {
            new Command("unsupported", _ => Task.FromResult(new CommandResult()))
            {
                Description = "Prevents stupid loops when a core messages another core",
                Aliases = new[] { "unknown" }
            },

            new Command("uptime", _ =>
            {
                Duration uptime = SystemClock.Instance.GetCurrentInstant() - _upSince;
                string uptimeFormatted = uptime.ToTimeSpan().ToHumanReadable(FormatPrecision.Adaptive);
                return Task.FromResult(new CommandResult { Response = $"TPP has been running for: {uptimeFormatted}" });
            }) { Description = "Shows how long the core has been running." }
        };
    }
}
