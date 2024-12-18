using System;
using System.Collections.Generic;
using NodaTime;

namespace TPP.Model;

public record DonationRecordBreakType(string Name, Duration Duration, int TokenWinning)
    : IComparable<DonationRecordBreakType>
{
    public int CompareTo(DonationRecordBreakType? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Duration.CompareTo(other.Duration);
    }
}

public static class DonationRecordBreaks
{
    public static readonly List<DonationRecordBreakType> Types =
    [
        new("1h", Duration.FromHours(1), 2),
        new("2h", Duration.FromHours(2), 2),
        new("6h", Duration.FromHours(6), 2),
        new("12h", Duration.FromHours(12), 2),
        new("24h", Duration.FromHours(24), 2),
        new("48h", Duration.FromHours(48), 10),
        new("3d", Duration.FromDays(3), 10),
        new("4d", Duration.FromDays(4), 10),
        new("5d", Duration.FromDays(5), 10),
        new("6d", Duration.FromDays(6), 10),
        new("7d", Duration.FromDays(7), 20),
        new("14d", Duration.FromDays(14), 20),
        new("30d", Duration.FromDays(30), 20),
        new("60d", Duration.FromDays(60), 20),
        new("90d", Duration.FromDays(90), 20)
    ];
}
