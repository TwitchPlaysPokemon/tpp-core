using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using Core.Chat;
using Core.Commands;
using Model;

namespace Core.Tests.Commands
{
    public class CommandResponderTest
    {
        private static User MockUser() => new User(
            id: Guid.NewGuid().ToString(),
            name: "User", twitchDisplayName: "☺user", simpleName: "user", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        [Test]
        public async Task TestRespondToSource()
        {
            // given
            var messageSenderMock = Substitute.For<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock);
            var user = MockUser();
            var chatMessage = new Message(user, "message text", new MessageSource.PrimaryChat(), string.Empty);
            var whisperMessage = new Message(user, "message text", new MessageSource.Whisper(), string.Empty);

            // when
            await commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.Source });
            await commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response!", ResponseTarget = ResponseTarget.Source });

            // then
            await messageSenderMock.Received(1).SendMessage("Chat response!", chatMessage);
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response!");
        }

        [Test]
        public async Task TestRespondFixed()
        {
            // given
            var messageSenderMock = Substitute.For<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock);
            var user = MockUser();
            var chatMessage = new Message(user, "message text", new MessageSource.PrimaryChat(), string.Empty);
            var whisperMessage = new Message(user, "message text", new MessageSource.Whisper(), string.Empty);

            // when
            await commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response 1!", ResponseTarget = ResponseTarget.Chat });
            await commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Chat response 2!", ResponseTarget = ResponseTarget.Chat });
            await commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Whisper response 1!", ResponseTarget = ResponseTarget.Whisper });
            await commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response 2!", ResponseTarget = ResponseTarget.Whisper });

            // then
            await messageSenderMock.Received(1).SendMessage("Chat response 1!", chatMessage);
            await messageSenderMock.Received(1).SendMessage("Chat response 2!", whisperMessage);
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response 1!");
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response 2!");
        }

        [Test]
        public async Task TestWhisperIfLong()
        {
            // given
            var messageSenderMock = Substitute.For<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock, whisperIfLongThreshold: 20);
            var user = MockUser();
            var chatMessageShort = new Message(user, "short", new MessageSource.PrimaryChat(), string.Empty);
            var chatMessageLong = new Message(user, "longer than 20 characters", new MessageSource.PrimaryChat(), string.Empty);
            var whisperMessageShort = new Message(user, "short", new MessageSource.Whisper(), string.Empty);
            var whisperMessageLong = new Message(user, "longer than 20 characters", new MessageSource.Whisper(), string.Empty);

            // when
            await commandResponder.ProcessResponse(chatMessageShort,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.WhisperIfLong });
            await commandResponder.ProcessResponse(chatMessageLong,
                new CommandResult { Response = "Too long chat response!", ResponseTarget = ResponseTarget.WhisperIfLong });
            await commandResponder.ProcessResponse(whisperMessageShort,
                new CommandResult { Response = "Whisper response 1!", ResponseTarget = ResponseTarget.WhisperIfLong });
            await commandResponder.ProcessResponse(whisperMessageLong,
                new CommandResult { Response = "Whisper response 2!", ResponseTarget = ResponseTarget.WhisperIfLong });

            // then
            await messageSenderMock.Received(1).SendMessage("Chat response!", chatMessageShort);
            await messageSenderMock.Received(1).SendWhisper(user, "Too long chat response!");
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response 1!");
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response 2!");
        }

        [Test]
        public async Task TestNoneIfChat()
        {
            // given
            var messageSenderMock = Substitute.For<IMessageSender>();
            var commandResponder = new CommandResponder(messageSenderMock);
            var user = MockUser();
            var chatMessage = new Message(user, "message text", new MessageSource.PrimaryChat(), string.Empty);
            var whisperMessage = new Message(user, "message text", new MessageSource.Whisper(), string.Empty);

            // when
            await commandResponder.ProcessResponse(chatMessage,
                new CommandResult { Response = "Chat response!", ResponseTarget = ResponseTarget.NoneIfChat });
            await commandResponder.ProcessResponse(whisperMessage,
                new CommandResult { Response = "Whisper response!", ResponseTarget = ResponseTarget.NoneIfChat });

            // then
            await messageSenderMock.Received(1).SendWhisper(user, "Whisper response!");
            Assert.That(messageSenderMock.ReceivedCalls().Count(), Is.EqualTo(1)); // no other calls
        }
    }
}
