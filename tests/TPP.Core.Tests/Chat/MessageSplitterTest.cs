using System.Linq;
using NUnit.Framework;
using TPP.Core.Chat;

namespace TPP.Core.Tests.Chat;

public class MessageSplitterTest
{
    [Test]
    public void DontSplitShortMessage()
    {
        var messageSplitter = new MessageSplitter(maxMessageLength: 100);
        const string message = "This message has few enough characters to not get split.";
        Assert.That(
            messageSplitter.FitToMaxLength(message).ToArray(), Is.EqualTo(new[] { message }));
    }

    [Test]
    public void SplitAtSpace()
    {
        var messageSplitter = new MessageSplitter(maxMessageLength: 30);
        const string message = "This message gets split into space-separated parts no longer than 30 chars.";
        Assert.That(
            messageSplitter.FitToMaxLength(message).ToArray(), Is.EqualTo(new[]
            {
                "This message gets split ...",
                "into space-separated parts ...",
                "no longer than 30 chars."
            }));
    }

    [Test]
    public void SplitAtMaxIfNoSpace()
    {
        var messageSplitter = new MessageSplitter(maxMessageLength: 30);
        const string message = "This-message-gets-forcefully-split-because-it-has-no-spaces.";
        Assert.That(
            messageSplitter.FitToMaxLength(message).ToArray(), Is.EqualTo(new[]
            {
                "This-message-gets-forcefull...",
                "y-split-because-it-has-no-s...",
                "paces."
            }));
    }

    [Test]
    public void LastSplitPerfectLength()
    {
        var messageSplitter = new MessageSplitter(maxMessageLength: 25);
        const string message = "This message's last part fits perfectly without continuation dots";
        Assert.That(
            messageSplitter.FitToMaxLength(message).ToArray(), Is.EqualTo(new[]
            {
                "This message's last ...",
                "part fits perfectly ...",
                "without continuation dots"
            }));
    }
}
