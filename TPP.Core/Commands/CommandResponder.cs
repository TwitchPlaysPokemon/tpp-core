using System;
using System.Threading.Tasks;
using TPP.Core.Chat;

namespace TPP.Core.Commands;

public interface ICommandResponder
{
    public Task ProcessResponse(Message message, CommandResult result);
}

public class CommandResponder : ICommandResponder
{
    private readonly IMessageSender _messageSender;
    private readonly int _whisperIfLongThreshold;

    public CommandResponder(
            IMessageSender messageSender, int whisperIfLongThreshold = 100)
    {
        _messageSender = messageSender;
        _whisperIfLongThreshold = whisperIfLongThreshold;
    }

    public async Task ProcessResponse(Message message, CommandResult result)
    {
        if (result.Response == null) return;
        Task RespondViaChat(string? customMessage = null) =>
                _messageSender.SendMessage(customMessage ?? result.Response, responseTo: message);
        Task RespondViaWhisper() => _messageSender.SendWhisper(message.User, result.Response);
        switch (result.ResponseTarget)
        {
            case ResponseTarget.Source:
                if (message.MessageSource == MessageSource.Chat)
                    await RespondViaChat();
                else if (message.MessageSource == MessageSource.Whisper)
                    await RespondViaWhisper();
                break;
            case ResponseTarget.Chat:
                await RespondViaChat();
                break;
            case ResponseTarget.Whisper:
                await RespondViaWhisper();
                break;
            case ResponseTarget.WhisperIfLong:
                if (message.MessageSource == MessageSource.Chat)
                {
                    if (result.Response.Length > _whisperIfLongThreshold)
                    {
                        await RespondViaChat(customMessage: " The reply has been whispered to you.");
                        await RespondViaWhisper();
                    }
                    else
                    {
                        await RespondViaChat();
                    }
                }
                else
                {
                    await RespondViaWhisper();
                }
                break;
            case ResponseTarget.NoneIfChat:
                if (message.MessageSource != MessageSource.Chat)
                    await RespondViaWhisper();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result.ResponseTarget));
        }
    }
}
