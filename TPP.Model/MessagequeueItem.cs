namespace TPP.Model;

/// <summary>
/// A message that gets persisted in the database for to the old core to read and execute.
/// This is a dual-core feature to allow for staggered message processing between both cores.
/// </summary>
public record MessagequeueItem(string Id, string IrcLine);
