using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface ICommandAliasRepo
{
    /// Get all aliases.
    public Task<IImmutableList<CommandAlias>> GetAliases();

    /// Insert or update an alias.
    public Task<CommandAlias> UpsertAlias(string alias, string targetCommand, string[] fixedArgs);

    /// Remove an alias by name. Returns whether that alias was found and removed.
    public Task<bool> RemoveAlias(string alias);

    /// An alias was inserted. Note that if an alias was replaced, a <see cref="AliasRemoved"/> event
    /// will have been fired for the alias beforehand.
    public event EventHandler<CommandAlias> AliasInserted;

    /// An alias was removed.
    public event EventHandler<string> AliasRemoved;
}
