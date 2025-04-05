namespace TPP.Core.Commands;

public enum ResponseTarget
{
    Source,
    Chat,
    Whisper,
    WhisperIfLong,
    NoneIfChat,
}

public class CommandResult
{
    public string? Response { get; init; } = null;
    public ResponseTarget ResponseTarget { get; init; } = ResponseTarget.Source;
}
