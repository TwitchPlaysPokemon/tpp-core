using System;
using System.Threading.Tasks;
using TPP.Core.Chat;

namespace TPP.Core.Commands
{
    public interface ICommandResponder
    {
        public Task ProcessResponse(Message message, CommandResult result);
    }

    public class CommandResponder : ICommandResponder
    {
        private readonly IMessageSender _messageSender;
        private readonly int _whisperIfLongThreshold;

        public CommandResponder(IMessageSender messageSender, int whisperIfLongThreshold = 100)
        {
            _messageSender = messageSender;
            _whisperIfLongThreshold = whisperIfLongThreshold;
        }

        public async Task ProcessResponse(Message message, CommandResult result)
        {
            if (result.Response == null) return;
            switch (result.ResponseTarget)
            {
                case ResponseTarget.Source:
                    if (message.MessageSource == MessageSource.Chat)
                        await _messageSender.SendMessage($"@{message.User.Name} {result.Response}");
                    else if (message.MessageSource == MessageSource.Whisper)
                        await _messageSender.SendWhisper(message.User, result.Response);
                    break;
                case ResponseTarget.Chat:
                    await _messageSender.SendMessage($"@{message.User.Name} {result.Response}");
                    break;
                case ResponseTarget.Whisper:
                    await _messageSender.SendWhisper(message.User, result.Response);
                    break;
                case ResponseTarget.WhisperIfLong:
                    if (message.MessageSource == MessageSource.Chat)
                    {
                        if (result.Response.Length > _whisperIfLongThreshold)
                        {
                            await _messageSender.SendMessage(
                                $"@{message.User.Name} The reply has been whispered to you.");
                            await _messageSender.SendWhisper(message.User, result.Response);
                        }
                        else
                        {
                            await _messageSender.SendMessage($"@{message.User.Name} {result.Response}");
                        }
                    }
                    else
                    {
                        await _messageSender.SendWhisper(message.User, result.Response);
                    }
                    break;
                case ResponseTarget.NoneIfChat:
                    if (message.MessageSource != MessageSource.Chat)
                        await _messageSender.SendWhisper(message.User, result.Response);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.ResponseTarget));
            }
        }
    }
}
