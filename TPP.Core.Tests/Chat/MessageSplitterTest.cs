using System.Linq;
using NUnit.Framework;
using TPP.Core.Chat;

namespace TPP.Core.Tests.Chat
{
    public class MessageSplitterTest
    {
        [Test]
        public void DontSplitShortMessage()
        {
            var messageSplitter = new MessageSplitter(maxMessageLength: 100);
            const string message = "This message has few enough characters to not get split.";
            Assert.AreEqual(
                new[] { message },
                messageSplitter.FitToMaxLength(message).ToArray());
        }

        [Test]
        public void SplitAtSpace()
        {
            var messageSplitter = new MessageSplitter(maxMessageLength: 30);
            const string message = "This message gets split into space-separated parts no longer than 30 chars.";
            Assert.AreEqual(
                new[]
                {
                    "This message gets split ...",
                    "into space-separated parts ...",
                    "no longer than 30 chars."
                },
                messageSplitter.FitToMaxLength(message).ToArray());
        }

        [Test]
        public void SplitAtMaxIfNoSpace()
        {
            var messageSplitter = new MessageSplitter(maxMessageLength: 30);
            const string message = "This-message-gets-forcefully-split-because-it-has-no-spaces.";
            Assert.AreEqual(
                new[]
                {
                    "This-message-gets-forcefull...",
                    "y-split-because-it-has-no-s...",
                    "paces."
                },
                messageSplitter.FitToMaxLength(message).ToArray());
        }

        [Test]
        public void LastSplitPerfectLength()
        {
            var messageSplitter = new MessageSplitter(maxMessageLength: 25);
            const string message = "This message's last part fits perfectly without continuation dots";
            Assert.AreEqual(
                new[]
                {
                    "This message's last ...",
                    "part fits perfectly ...",
                    "without continuation dots"
                },
                messageSplitter.FitToMaxLength(message).ToArray());
        }
    }
}
