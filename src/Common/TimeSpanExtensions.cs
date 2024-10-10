using System;

namespace Common;

public enum FormatPrecision { Days, Hours, Minutes, Seconds, Milliseconds, Adaptive }
public static class TimeSpanExtensions
{
    public static string ToHumanReadable(
        this TimeSpan timeSpan, FormatPrecision precision = FormatPrecision.Milliseconds)
    {
        if (precision == FormatPrecision.Adaptive)
        {
            if (timeSpan.TotalSeconds >= 1) precision = FormatPrecision.Milliseconds;
            if (timeSpan.TotalMinutes >= 1) precision = FormatPrecision.Seconds;
            if (timeSpan.TotalHours >= 1) precision = FormatPrecision.Minutes;
            if (timeSpan.TotalDays >= 1) precision = FormatPrecision.Hours;
        }

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
