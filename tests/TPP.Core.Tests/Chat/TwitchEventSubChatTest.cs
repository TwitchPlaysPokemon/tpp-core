using System.Collections.Immutable;
using NUnit.Framework;
using TPP.Core.Chat;
using TPP.Twitch.EventSub.Notifications;

namespace TPP.Core.Tests.Chat;

[TestFixture]
[TestOf(typeof(TwitchEventSubChat))]
public class TwitchEventSubChatTest
{
    /// <summary>
    /// The data comes from an actual subscription that was observed.
    /// See also <see cref="ChannelSubscriptionMessage.Emote"/>
    /// </summary>
    [Test]
    public void ParseSubscriptionMessageEmotes()
    {
        const string message = "test Kappa 🌿 BabyRage 🐍 PogChamp 🎅🏿 RaccAttack ♥ PraiseIt 12345678901234567890";
        ChannelSubscriptionMessage.Emote[] emotes =
        [
            new(5, 9, "25"),
            new(16, 23, "22639"),
            new(30, 37, "305954156"),
            new(48, 57, "114870"),
            new(63, 70, "38586")
        ];
        ImmutableList<EmoteOccurrence> occurrences = TwitchEventSubChat.ParseEmotes(emotes, message);
        ImmutableList<EmoteOccurrence> expected = [
            new("25", "Kappa"),
            new("22639", "BabyRage"),
            new("305954156", "PogChamp"),
            new("114870", "RaccAttack"),
            new("38586", "PraiseIt")
        ];
        Assert.That(occurrences, Is.EquivalentTo(expected));
    }
}
