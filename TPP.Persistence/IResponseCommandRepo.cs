namespace TPP.Persistence;

public interface IResponseCommandRepo
{
    /// Get all commands.
    public Task<IImmutableList<ResponseCommand>> GetCommands();

    /// Insert or update a command.
    public Task<ResponseCommand> UpsertCommand(string command, string response);

    /// Remove a command by name. Returns whether that command was found and removed.
    public Task<bool> RemoveCommand(string command);

    /// A command was inserted. Note that if a command was replaced, a <see cref="CommandRemoved"/> event
    /// will have been fired for the command beforehand.
    public event EventHandler<ResponseCommand> CommandInserted;

    /// A command was removed.
    public event EventHandler<string> CommandRemoved;
}
