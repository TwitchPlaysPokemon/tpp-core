using System;

namespace TPP.Common;

public enum FormatPrecision { Days, Hours, Minutes, Seconds, Milliseconds }
public static class TimeSpanExtensions
{
    public static string ToHumanReadable(
        this TimeSpan timeSpan, FormatPrecision precision = FormatPrecision.Milliseconds)
    {
        string result = "";
        if (precision >= FormatPrecision.Days && timeSpan.Days > 0)
            result += $"{timeSpan.Days}d";
        if (precision >= FormatPrecision.Hours && timeSpan.Hours > 0)
            result += $"{timeSpan.Hours}h";
        if (precision >= FormatPrecision.Minutes && timeSpan.Minutes > 0)
            result += $"{timeSpan.Minutes}m";
        if (precision >= FormatPrecision.Seconds && timeSpan.Seconds > 0)
            result += $"{timeSpan.Seconds}s";
        if (precision >= FormatPrecision.Milliseconds && timeSpan.Milliseconds > 0)
            result += $"{timeSpan.Milliseconds}ms";
        if (result == "") result = "0s";
        return result;
    }
}
