namespace TPP.Common.Utils;

public static class StringExtensions
{
    public static string Genitive(this string self) =>
        self.Length > 0 && self[^1] == 's'
            ? self + "'"
            : self + "'s";
}
