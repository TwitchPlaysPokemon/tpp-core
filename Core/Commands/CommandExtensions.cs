using System.Threading.Tasks;

namespace Core.Commands
{
    public static class CommandExtensions
    {
        public static Task<T1> ParseArgs<T1>(this CommandContext ctx) =>
            ctx.ArgsParser.Parse<T1>(ctx.Args);

        public static Task<(T1, T2)> ParseArgs<T1, T2>(this CommandContext ctx) =>
            ctx.ArgsParser.Parse<T1, T2>(ctx.Args);

        public static Task<(T1, T2, T3)> ParseArgs<T1, T2, T3>(this CommandContext ctx) =>
            ctx.ArgsParser.Parse<T1, T2, T3>(ctx.Args);

        public static Task<(T1, T2, T3, T4)> ParseArgs<T1, T2, T3, T4>(this CommandContext ctx) =>
            ctx.ArgsParser.Parse<T1, T2, T3, T4>(ctx.Args);
    }
}
