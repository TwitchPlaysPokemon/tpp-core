using System;
using System.Threading.Tasks;
using TPP.Core.Chat;

namespace TPP.Core.Commands;

public interface ICommandResponder
{
    public Task ProcessResponse(Message message, CommandResult result);
}

public class CommandResponder(IMessageSender messageSender, int whisperIfLongThreshold = 100)
    : ICommandResponder
{
    public async Task ProcessResponse(Message message, CommandResult result)
    {
        if (result.Response == null) return;
        Task RespondViaChat(string? customMessage = null) =>
            messageSender.SendMessage(customMessage ?? result.Response, responseTo: message);
        Task RespondViaWhisper() => messageSender.SendWhisper(message.User, result.Response);
        switch (result.ResponseTarget)
        {
            case ResponseTarget.Source:
                if (message.MessageSource is MessageSource.PrimaryChat)
                    await RespondViaChat();
                else if (message.MessageSource is MessageSource.Whisper or MessageSource.SecondaryChat)
                    await RespondViaWhisper();
                break;
            case ResponseTarget.Chat:
                await RespondViaChat();
                break;
            case ResponseTarget.Whisper:
                await RespondViaWhisper();
                break;
            case ResponseTarget.WhisperIfLong:
                if (message.MessageSource is not MessageSource.Whisper)
                {
                    if (result.Response.Length > whisperIfLongThreshold)
                    {
                        await RespondViaChat(customMessage: "The reply has been whispered to you.");
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
                if (message.MessageSource is not MessageSource.PrimaryChat)
                    await RespondViaWhisper();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result.ResponseTarget));
        }
    }
}
