using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class MessageTest
    {
        [Fact]
        public void TestParseMessagePlain()
        {
            var message = new Message();
            message.ParseFrom("PRIVMSG #Example :This is a sample message");

            Assert.Equal("PRIVMSG", message.Command);
            Assert.Equal("#Example", message.Parameters[0]);
            Assert.Equal("#Example", message.Target);
            Assert.Equal("#example", message.TargetLower);
            Assert.Equal("This is a sample message", message.Parameters[1]);
            Assert.Equal("This is a sample message", message.TrailingParameter);
        }

        [Fact]
        public void TestParseMessagePrefixUser()
        {
            var message = new Message();
            message.ParseFrom(":nick!user@host.tmi.twitch.tv PRIVMSG #Example :This is a sample message");

            Assert.Equal("nick", message.Prefix.ClientId.Nickname);
            Assert.Equal("host.tmi.twitch.tv", message.Prefix.ClientId.Host);

            Assert.Equal("PRIVMSG", message.Command);
            Assert.Equal("#Example", message.Parameters[0]);
            Assert.Equal("This is a sample message", message.Parameters[1]);
        }

        [Fact]
        public void TestParseMessagePrefixServer()
        {
            var message = new Message();
            message.ParseFrom(":example.com NOTICE meNick :The server is rebooting soon!");

            Assert.Equal("example.com", message.Prefix.Server);

            Assert.Equal("NOTICE", message.Command);
            Assert.Equal("meNick", message.Parameters[0]);
            Assert.Equal("meNick", message.Target);
            Assert.Equal("menick", message.TargetLower);
            Assert.Equal("The server is rebooting soon!", message.Parameters[1]);
        }

        [Fact]
        public void TestParseMessageNumericalReply()
        {
            var message = new Message();
            message.ParseFrom(":example.com 353 ronni = #dallas :ronni fred wilma");

            Assert.Equal("example.com", message.Prefix.Server);

            Assert.Equal(353, message.NumericReply);
            Assert.Equal("ronni", message.Parameters[0]);
            Assert.Equal("=", message.Parameters[1]);
            Assert.Equal("#dallas", message.Parameters[2]);
            Assert.Equal("ronni fred wilma", message.Parameters[3]);
        }

        [Fact]
        public void TestParseMessageTags()
        {
            var message = new Message();
            message.ParseFrom("@color=#0D4200;display-name=dallas;emote-sets=0,33,50,237,793,2126,3517,4578,5569,9400,10337,12239;turbo=0;user-id=1337;user-type=admin :tmi.twitch.tv GLOBALUSERSTATE");

            Assert.Equal(6, message.Tags.Count);
            Assert.Equal("dallas", message.Tags["display-name"]);
            Assert.Equal("tmi.twitch.tv", message.Prefix.Server);
            Assert.Equal("GLOBALUSERSTATE", message.Command);
            Assert.Empty(message.Parameters);
        }

        [Fact]
        public void TestParseCtcpMessage()
        {
            var message = new Message();
            message.ParseFrom(":dan!u@localhost PRIVMSG #ircv3 :\u0001ACTION writes some specs!\u0001");

            Assert.Equal("PRIVMSG", message.Command);
            Assert.Equal("ACTION", message.CtcpMessage.Command);
            Assert.Equal("writes some specs!", message.CtcpMessage.Parameters);
        }

        [Fact]
        public void TestToString() {
            var message = new Message("PRIVMSG", new[] {"#dallas"}, "hello world!");
            Assert.Equal("PRIVMSG #dallas :hello world!", message.ToString());

            message = new Message("JOIN", new[] {"#dallas"});
            Assert.Equal("JOIN #dallas", message.ToString());

            // Don't do this in real IRC conversation.
            // You shouldn't send a prefix, any unknown parameters, or
            // unexpected tags.
            message = new Message("NOTICE", new[] {"#dallas", "blah"},
                "Hello! I'm a poorly programmed IRC client.");
            message.Prefix.ClientId = new ClientId("me", "me", "example.com");
            message.Tags["admin"] = "1";
            Assert.Equal("@admin=1 :me!me@example.com NOTICE #dallas blah :Hello! I'm a poorly programmed IRC client.",
                message.ToString());
        }

        [Fact]
        public void TestParseBadInput()
        {
            var message = new Message();

            Assert.Throws<IrcParserException>(() => message.ParseFrom(":"));
        }

        [Fact]
        public void TestBadOutput()
        {
            var message = new Message("PRIVMSG", new[] {"#example"},
                "Hi!\r\nPRIVMSG #example :I'm a poorly coded bot!");

            Assert.Throws<IrcException>(() => message.ToString());
        }
    }
}
