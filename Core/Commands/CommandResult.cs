namespace Core.Commands
{
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
        public string? Response { get; set; } = null;
        public ResponseTarget ResponseTarget { get; set; } = ResponseTarget.Source;
    }
}
