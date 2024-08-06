using System.Collections.Generic;

namespace TPP.Core.Commands.Definitions;

/// <summary>
/// Because command definitions are usually split up into separate classes with a few commands each,
/// they can implement this interface to allow users to more easily just get all available commands.
/// This interface exists purely for convenience.
/// </summary>
public interface ICommandCollection
{
    IEnumerable<Command> Commands { get; }
}
