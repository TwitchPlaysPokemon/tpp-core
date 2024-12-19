using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class DonationCommands(
    IDonationRepo donationRepo,
    IClock clock
) : ICommandCollection
{
    public IEnumerable<Command> Commands =>
    [
        new("donationrecords", DonationRecords)
        {
            Aliases = ["tiprecords"],
            Description =
                "Shows the current donation records. " +
                "Check !support for how to tip, and get bonus token for every record you break!"
        }
    ];

    public async Task<CommandResult> DonationRecords(CommandContext context)
    {
        SortedDictionary<DonationRecordBreakType, Donation> recordBreaks =
            await donationRepo.GetRecordDonations(clock.GetCurrentInstant());

        IEnumerable<string> recordStrings = DonationRecordBreaks.Types
            .Select(recordBreakType =>
            {
                Donation? donation = recordBreaks.GetValueOrDefault(recordBreakType);
                string x = donation == null
                    ? "no donation"
                    : "$" + (donation.Cents / 100f).ToString("F2") + " by " + donation.UserName;
                return recordBreakType.Name + ": " + x;
            });
        string response = "Current donation records: " + string.Join(", ", recordStrings);
        return new CommandResult { Response = response };
    }
}
