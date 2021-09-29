namespace TPP.Persistence;

public interface ICommandLogger
{
    public Task<CommandLog> Log(string userId, string command, IImmutableList<string> args, string? response);
}
