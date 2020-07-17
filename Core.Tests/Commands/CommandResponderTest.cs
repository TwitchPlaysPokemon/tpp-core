using Core.Chat;
using Core.Commands;
using Moq;
using NUnit.Framework;
using static Core.Tests.TestUtils;

namespace Core.Tests.Commands
{
    public class CommandResponderTest
    {
        [Test]
        public void TestRespondToSource()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object);
            var user = MockUser("user");
            var chatMessage = new Message(user, "message text", MessageSource.Chat);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper);

            // when
            commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.Source });
            commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response!", ResponseTarget = ResponseTarget.Source });

            // then
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.TwitchDisplayName} Chat response!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response!"), Times.Once);
        }

        [Test]
        public void TestRespondFixed()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object);
            var user = MockUser("user");
            var chatMessage = new Message(user, "message text", MessageSource.Chat);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper);

            // when
            commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response 1!", ResponseTarget = ResponseTarget.Chat });
            commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Chat response 2!", ResponseTarget = ResponseTarget.Chat });
            commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Whisper response 1!", ResponseTarget = ResponseTarget.Whisper });
            commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response 2!", ResponseTarget = ResponseTarget.Whisper });

            // then
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.TwitchDisplayName} Chat response 1!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.TwitchDisplayName} Chat response 2!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 1!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 2!"), Times.Once);
        }

        [Test]
        public void TestWhisperIfLong()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object, whisperIfLongThreshold: 20);
            var user = MockUser("user");
            var chatMessageShort = new Message(user, "short", MessageSource.Chat);
            var chatMessageLong = new Message(user, "longer than 20 characters", MessageSource.Chat);
            var whisperMessageShort = new Message(user, "short", MessageSource.Whisper);
            var whisperMessageLong = new Message(user, "longer than 20 characters", MessageSource.Whisper);

            // when
            commandResponder.ProcessResponse(chatMessageShort,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.WhisperIfLong });
            commandResponder.ProcessResponse(chatMessageLong,
                new CommandResult { Response = "Too long chat response!", ResponseTarget = ResponseTarget.WhisperIfLong });
            commandResponder.ProcessResponse(whisperMessageShort,
                new CommandResult { Response = "Whisper response 1!", ResponseTarget = ResponseTarget.WhisperIfLong });
            commandResponder.ProcessResponse(whisperMessageLong,
                new CommandResult { Response = "Whisper response 2!", ResponseTarget = ResponseTarget.WhisperIfLong });

            // then
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.TwitchDisplayName} Chat response!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Too long chat response!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 1!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 2!"), Times.Once);
        }

        [Test]
        public void TestNoneIfChat()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object);
            var user = MockUser("user");
            var chatMessage = new Message(user, "message text", MessageSource.Chat);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper);

            // when
            commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.NoneIfChat });
            commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response!", ResponseTarget = ResponseTarget.NoneIfChat });

            // then
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response!"), Times.Once);
            messageSenderMock.VerifyNoOtherCalls();
        }
    }
}
