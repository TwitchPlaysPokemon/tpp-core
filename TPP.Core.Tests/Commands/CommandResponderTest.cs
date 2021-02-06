using System;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Persistence.Models;

namespace TPP.Core.Tests.Commands
{
    public class CommandResponderTest
    {
        private static User MockUser() => new User(
            id: Guid.NewGuid().ToString(),
            name: "User", twitchDisplayName: "☺user", simpleName: "user", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        [Test]
        public void TestRespondToSource()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object);
            var user = MockUser();
            var chatMessage = new Message(user, "message text", MessageSource.Chat, string.Empty);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper, string.Empty);

            // when
            commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.Source });
            commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response!", ResponseTarget = ResponseTarget.Source });

            // then
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.Name} Chat response!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response!"), Times.Once);
        }

        [Test]
        public void TestRespondFixed()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object);
            var user = MockUser();
            var chatMessage = new Message(user, "message text", MessageSource.Chat, string.Empty);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper, string.Empty);

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
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.Name} Chat response 1!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.Name} Chat response 2!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 1!"), Times.Once);
            messageSenderMock.Verify(ms => ms.SendWhisper(user, "Whisper response 2!"), Times.Once);
        }

        [Test]
        public void TestWhisperIfLong()
        {
            // given
            var messageSenderMock = new Mock<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock.Object, whisperIfLongThreshold: 20);
            var user = MockUser();
            var chatMessageShort = new Message(user, "short", MessageSource.Chat, string.Empty);
            var chatMessageLong = new Message(user, "longer than 20 characters", MessageSource.Chat, string.Empty);
            var whisperMessageShort = new Message(user, "short", MessageSource.Whisper, string.Empty);
            var whisperMessageLong = new Message(user, "longer than 20 characters", MessageSource.Whisper, string.Empty);

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
            messageSenderMock.Verify(ms => ms.SendMessage($"@{user.Name} Chat response!"), Times.Once);
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
            var user = MockUser();
            var chatMessage = new Message(user, "message text", MessageSource.Chat, string.Empty);
            var whisperMessage = new Message(user, "message text", MessageSource.Whisper, string.Empty);

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
