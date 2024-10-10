using NodaTime;

namespace Model;

public enum MessageType { Chat, Whisper }

/// <summary>
/// A message that the old core persisted in the database for us to read and process.
/// This is a dual-core feature to allow the old core to send messages through the new core,
/// so it doesn't have to deal with twitch chat communications by itself.
/// </summary>
public record IncomingMessagequeueItem(
    string Id,
    string Message,
    MessageType MessageType,
    string Target,
    Instant QueuedAt);
