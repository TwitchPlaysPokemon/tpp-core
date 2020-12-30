using System;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Core.Moderation;
using TPP.Persistence.Models;

namespace TPP.Core.Tests.Moderation
{
    public static class RulesTest
    {
        private static User MockUser(string name) => new(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        private static Message TextMessage(string text) => new(MockUser("MockUser"), text, MessageSource.Chat, string.Empty);

        [TestFixture]
        private class Copypasta
        {
            [Test]
            public void detects_recent_exact_copypasta()
            {
                Mock<IClock> clockMock = new();
                CopypastaRule rule = new(clockMock.Object, Duration.FromSeconds(10));

                const string copypasta =
                    "Please do not copy and paste this copypasta. It is my original copypasta and is protected by copyright law. If I see anyone pasting my intellectual property without permission, a navy seal trained in gorilla warfare will smite you.";

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultCopypasted = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.GivePoints>(resultCopypasted);

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(16));
                RuleResult resultLapsed = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultLapsed);
            }

            [Test]
            public void detects_similar_copypasta()
            {
                Mock<IClock> clockMock = new();
                CopypastaRule rule = new(clockMock.Object, Duration.FromSeconds(10));

                const string copypasta1 =
                    "What the *** did you just *** type about me, you little bitch? I’ll have you know I graduated top of my class at MIT, and I’ve been involved in numerous secret raids with Anonymous, and I have over 300 confirmed DDoSes.";
                const string copypasta2 =
                    "What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills.";

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta1));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultSimilarCopypasted = rule.Check(TextMessage(copypasta2));
                Assert.IsInstanceOf<RuleResult.GivePoints>(resultSimilarCopypasted);
            }

            [Test]
            public void ignores_different_messages()
            {
                Mock<IClock> clockMock = new();
                CopypastaRule rule = new(clockMock.Object, Duration.FromSeconds(10));

                const string copypasta1 =
                    "What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills.";
                const string copypasta2 =
                    "Welch eynen verschissenen Unfug schicktest du dich zur Hölle nochmal an, über das heilige römische Reych in die Welt herauszuthragen, du Lustknabe? Seyd drumb in Kennthnisz gesetzet, dass min threue Sünderseele meynes Gewalthauvns besther Landsknecht gewesen und an Schwerthzügen gegen holländische Rebellen meynen Theil trug, derer nicht nur zahlreych, sondern auch occulter Natura waren.";

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta1));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultSimilarCopypasted = rule.Check(TextMessage(copypasta2));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultSimilarCopypasted);
            }
        }

        [TestFixture]
        private class UnicodeCharacterCategory
        {
            [Test]
            public void detects_zalgo()
            {
                UnicodeCharacterCategoryRule rule = new(badnessPointsMultiplier: 200);
                RuleResult result = rule.Check(TextMessage(
                    "A҉̳͎̘̮͚̖ ̮̘̮͉̭̕m̕a̸̻̺̗n̙͉̹̣̝̜̰ ͍̩̰̬͇i̷̺͇͈̼̝s͇ s̰͇͎̱̼̥t̵̪̳̖͔̟ị̣̥̫͓̤̭̕l̯̣̹ḽ̸͓ i̥̱̬͚n̤͍ ̥̮̭͎͔͞c̷̼͇̦̪̤̜̖r͎͎̟̱͙i̵͓̖̫̱̫̘̦t͔͕̠̻i͓̱̙̯c͈̭̣a͏l̠̳̝͟ ̭̳͖̤̭̯c҉o҉͈͈̠̙͔ͅn̨̗͖͈̝͇̜d̯̠͇͜ͅi̟͚͉͇̣t̴̜̫͇͉i̠͎̮̯̙͍̤o̧̺̹̝̫̰̯̻n̻͈͚͚̪̗̦ ̧͈͍̟̝̠͚̞a҉̩̻͈f̶t̗̞͙̭e̱͖̳̪͖ͅr͕ ̣s̝w̖̰̤͍̱̳͞a͕̪ͅl̳̫̬͕̞͡l̗̘ọ̵̺͖͍̗͍̺w̻̰̙̕ͅi̻͈͍̙͚̙͇n̲͈̩̝̠̺g̝̻͝ ͍̝̰̻̖̺͘$̶̫2͕̹̫̘5̶0̴̦,͚̖00̡̺͚̭̩͉0̙̪͠ ̠͉͓̭͖͚inͅ ̡͖̩͚̖͉̺̖l͖̞͍̼̟a͖̞̜r͈̘͖̺̬̼g̻̦̭̩̪e̼̠͍̩̤̬ ̸̦ͅb̞̱i͍̮̯l̟̳̺͘l̴͕͕̙̘s͏̭̫̤̝͓͇̘.̺ ͇̰̹̹̙̗̥N҉̟̼̘̘o̵͍̹̰̻̼̱ ̳͚̥̻͚c̮h̷̠̰̞̣̦͙a̷̮̮̘͈̪̘n͔̞͜ge̱͈̮͝s̱͓̣͉͔ ̗͖e̫̗̰̗̜x̗͖͉̗͚͍̼p̪̜̺̦͓͍̯e̝̰͚̕ct̺̞͇͔e̲͍̝d̥̬̮͉.̥̠̜̹̠"));
                Assert.IsInstanceOf<RuleResult.GivePoints>(result);
                Assert.IsTrue(((RuleResult.GivePoints)result).Points > 150);
            }

            [Test]
            public void detects_ascii_art()
            {
                UnicodeCharacterCategoryRule rule = new(badnessPointsMultiplier: 200);
                RuleResult result = rule.Check(TextMessage(
                    "⣿⣿⣿⣿⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠛⠻⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣦⠀⠀⠀⠀⠀⠀⠀⠀⢀⣤⣄⡀⠀⢻⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣇⠀⠀⠀⠀⠀⠀⠀⠸⣿⣿⣿⠃⢰⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣼⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⡆⠀⠀⠀⠀⠀⠀⢶⣶⣶⣾⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣧⠀⢠⡀⠐⠀⠀⠀⠻⢿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⡄⢸⣷⡄⠀⠣⣄⡀⠀⠉⠛⢿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣇⠀⣿⣿⣦⠀⠹⣿⣷⣶⣦⣼⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣼⣿⣿⣿⣷⣄⣸⣿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⡿⢛⡙⢻⠛⣉⢻⣉⢈⣹⣿⣿⠟⣉⢻⡏⢛⠙⣉⢻⣿⣿⣿ ⣿⣿⣇⠻⠃⣾⠸⠟⣸⣿⠈⣿⣿⣿⡀⠴⠞⡇⣾⡄⣿⠘⣿⣿⣿ ⣿⣿⣟⠛⣃⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿"));
                Assert.IsInstanceOf<RuleResult.GivePoints>(result);
                Assert.IsTrue(((RuleResult.GivePoints)result).Points > 150);
            }

            [Test]
            public void ignores_short_messages()
            {
                UnicodeCharacterCategoryRule rule = new();
                RuleResult result = rule.Check(TextMessage("♫ ┌༼ຈل͜ຈ༽┘ ♪ DANCE RIOT ♪ └༼ຈل͜ຈ༽┐♫"));
                Assert.IsInstanceOf<RuleResult.Nothing>(result);
            }
        }
    }
}
