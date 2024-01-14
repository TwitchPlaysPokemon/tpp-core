using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.Core.Moderation;
using TPP.Model;

namespace TPP.Core.Tests.Moderation
{
    public static class RulesTest
    {
        private static User MockUser(string name) => new(
            id: name.GetHashCode().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        private static Message TextMessage(string text) =>
            new(MockUser("MockUser"), text, MessageSource.Chat, string.Empty);

        [TestFixture]
        private class Copypasta
        {
            [Test]
            public void detects_recent_exact_copypasta()
            {
                var clockMock = Substitute.For<IClock>();
                CopypastaRule rule = new(clockMock, Duration.FromSeconds(10));

                const string copypasta =
                    "Please do not copy and paste this copypasta. It is my original copypasta and is protected by copyright law. If I see anyone pasting my intellectual property without permission, a navy seal trained in gorilla warfare will smite you.";

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultCopypasted = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.GivePoints>(resultCopypasted);

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(16));
                RuleResult resultLapsed = rule.Check(TextMessage(copypasta));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultLapsed);
            }

            [Test]
            public void detects_similar_copypasta()
            {
                var clockMock = Substitute.For<IClock>();
                CopypastaRule rule = new(clockMock, Duration.FromSeconds(10));

                const string copypasta1 =
                    "What the *** did you just *** type about me, you little bitch? I’ll have you know I graduated top of my class at MIT, and I’ve been involved in numerous secret raids with Anonymous, and I have over 300 confirmed DDoSes.";
                const string copypasta2 =
                    "What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills.";

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta1));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultSimilarCopypasted = rule.Check(TextMessage(copypasta2));
                Assert.IsInstanceOf<RuleResult.GivePoints>(resultSimilarCopypasted);
            }

            [Test]
            public void ignores_different_messages()
            {
                var clockMock = Substitute.For<IClock>();
                CopypastaRule rule = new(clockMock, Duration.FromSeconds(10));

                const string copypasta1 =
                    "What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills.";
                const string copypasta2 =
                    "Welch eynen verschissenen Unfug schicktest du dich zur Hölle nochmal an, über das heilige römische Reych in die Welt herauszuthragen, du Lustknabe? Seyd drumb in Kennthnisz gesetzet, dass min threue Sünderseele meynes Gewalthauvns besther Landsknecht gewesen und an Schwerthzügen gegen holländische Rebellen meynen Theil trug, derer nicht nur zahlreych, sondern auch occulter Natura waren.";

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
                RuleResult resultFirstSeen = rule.Check(TextMessage(copypasta1));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultFirstSeen);

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(5));
                RuleResult resultSimilarCopypasted = rule.Check(TextMessage(copypasta2));
                Assert.IsInstanceOf<RuleResult.Nothing>(resultSimilarCopypasted);
            }
        }

        [TestFixture]
        private class PersonalRepetition
        {
            [Test]
            public void detects_counting_spam()
            {
                var clockMock = Substitute.For<IClock>();
                PersonalRepetitionRule rule = new(clockMock, recentMessagesTtl: Duration.FromSeconds(10));

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
                Assert.That(rule.Check(TextMessage("I AM INVINCIBLE TriHard I AM UNBANNABLE TriHard 1")),
                    Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("I AM INVINCIBLE TriHard I AM UNBANNABLE TriHard 2")),
                    Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("I AM INVINCIBLE TriHard I AM UNBANNABLE TriHard 3")),
                    Is.InstanceOf<RuleResult.DeleteMessage>());
                RuleResult result = rule.Check(TextMessage("I AM INVINCIBLE TriHard I AM UNBANNABLE TriHard 4"));
                Assert.That(result, Is.InstanceOf<RuleResult.Timeout>());
                Assert.That(((RuleResult.Timeout)result).Message, Is.EqualTo("excessively repetitious messages"));

                clockMock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(15));
                Assert.That(rule.Check(TextMessage("I AM INVINCIBLE TriHard I AM UNBANNABLE TriHard 5")),
                    Is.InstanceOf<RuleResult.Nothing>()); // previous messages expired
            }

            [Test]
            public void ignores_short_messages()
            {
                PersonalRepetitionRule rule = new(Substitute.For<IClock>());
                Assert.That(rule.Check(TextMessage("59,95+e+A-")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("59,95+e+A-")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("59,95+e+A-")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("59,95+e+A-")), Is.InstanceOf<RuleResult.Nothing>());
            }

            [Test]
            public void detects_short_non_input_messages()
            {
                PersonalRepetitionRule rule = new(Substitute.For<IClock>());
                Assert.That(rule.Check(TextMessage("I am eternal 1")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("I am eternal 2")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("I am eternal 3")), Is.InstanceOf<RuleResult.DeleteMessage>());
                RuleResult result = rule.Check(TextMessage("I am eternal 4"));
                Assert.That(result, Is.InstanceOf<RuleResult.Timeout>());
                Assert.That(((RuleResult.Timeout)result).Message, Is.EqualTo("excessively repetitious messages"));
            }

            [Test]
            public void ignores_commands()
            {
                PersonalRepetitionRule rule = new(Substitute.For<IClock>());
                Assert.That(rule.Check(TextMessage("!buybadge pidgey 20 t2 90d")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("!buybadge pidgey 20 t2 90d")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("!buybadge pidgey 20 t2 90d")), Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("!buybadge pidgey 20 t2 90d")), Is.InstanceOf<RuleResult.Nothing>());
            }

            [Test]
            public void ignores_different_messages()
            {
                PersonalRepetitionRule rule = new(Substitute.For<IClock>());
                Assert.That(rule.Check(TextMessage("Lorem ipsum dolor sit amet, consetetur sadipscing elitr")),
                    Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("sed diam nonumy eirmod tempor invidunt ut labore et dolore magna")),
                    Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("At vero eos et accusam et justo duo dolores et ea rebum")),
                    Is.InstanceOf<RuleResult.Nothing>());
                Assert.That(rule.Check(TextMessage("Stet clita kasd gubergren, no sea takimata sanctus est")),
                    Is.InstanceOf<RuleResult.Nothing>());
            }
        }

        [TestFixture]
        private class UnicodeCharacterCategory
        {
            [Test]
            public void detects_zalgo()
            {
                UnicodeCharacterCategoryRule rule = new(pointsPerBadChar: 2);
                RuleResult result = rule.Check(TextMessage(
                    "A҉̳͎̘̮͚̖ ̮̘̮͉̭̕m̕a̸̻̺̗n̙͉̹̣̝̜̰ ͍̩̰̬͇i̷̺͇͈̼̝s͇ s̰͇͎̱̼̥t̵̪̳̖͔̟ị̣̥̫͓̤̭̕l̯̣̹ḽ̸͓ i̥̱̬͚n̤͍ ̥̮̭͎͔͞c̷̼͇̦̪̤̜̖r͎͎̟̱͙i̵͓̖̫̱̫̘̦t͔͕̠̻i͓̱̙̯c͈̭̣a͏l̠̳̝͟ ̭̳͖̤̭̯c҉o҉͈͈̠̙͔ͅn̨̗͖͈̝͇̜d̯̠͇͜ͅi̟͚͉͇̣t̴̜̫͇͉i̠͎̮̯̙͍̤o̧̺̹̝̫̰̯̻n̻͈͚͚̪̗̦ ̧͈͍̟̝̠͚̞a҉̩̻͈f̶t̗̞͙̭e̱͖̳̪͖ͅr͕ ̣s̝w̖̰̤͍̱̳͞a͕̪ͅl̳̫̬͕̞͡l̗̘ọ̵̺͖͍̗͍̺w̻̰̙̕ͅi̻͈͍̙͚̙͇n̲͈̩̝̠̺g̝̻͝ ͍̝̰̻̖̺͘$̶̫2͕̹̫̘5̶0̴̦,͚̖00̡̺͚̭̩͉0̙̪͠ ̠͉͓̭͖͚inͅ ̡͖̩͚̖͉̺̖l͖̞͍̼̟a͖̞̜r͈̘͖̺̬̼g̻̦̭̩̪e̼̠͍̩̤̬ ̸̦ͅb̞̱i͍̮̯l̟̳̺͘l̴͕͕̙̘s͏̭̫̤̝͓͇̘.̺ ͇̰̹̹̙̗̥N҉̟̼̘̘o̵͍̹̰̻̼̱ ̳͚̥̻͚c̮h̷̠̰̞̣̦͙a̷̮̮̘͈̪̘n͔̞͜ge̱͈̮͝s̱͓̣͉͔ ̗͖e̫̗̰̗̜x̗͖͉̗͚͍̼p̪̜̺̦͓͍̯e̝̰͚̕ct̺̞͇͔e̲͍̝d̥̬̮͉.̥̠̜̹̠"));
                Assert.IsInstanceOf<RuleResult.GivePoints>(result);
                Assert.IsTrue(((RuleResult.GivePoints)result).Points > 150);
            }

            [Test]
            public void detects_ascii_art()
            {
                UnicodeCharacterCategoryRule rule = new(pointsPerBadChar: 2);
                RuleResult result = rule.Check(TextMessage(
                    "⣿⣿⣿⣿⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠛⠻⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣦⠀⠀⠀⠀⠀⠀⠀⠀⢀⣤⣄⡀⠀⢻⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣇⠀⠀⠀⠀⠀⠀⠀⠸⣿⣿⣿⠃⢰⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣼⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⡆⠀⠀⠀⠀⠀⠀⢶⣶⣶⣾⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣧⠀⢠⡀⠐⠀⠀⠀⠻⢿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⡄⢸⣷⡄⠀⠣⣄⡀⠀⠉⠛⢿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣇⠀⣿⣿⣦⠀⠹⣿⣷⣶⣦⣼⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣼⣿⣿⣿⣷⣄⣸⣿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿ ⣿⣿⡿⢛⡙⢻⠛⣉⢻⣉⢈⣹⣿⣿⠟⣉⢻⡏⢛⠙⣉⢻⣿⣿⣿ ⣿⣿⣇⠻⠃⣾⠸⠟⣸⣿⠈⣿⣿⣿⡀⠴⠞⡇⣾⡄⣿⠘⣿⣿⣿ ⣿⣿⣟⠛⣃⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣿⣿⣿⣿⣿ ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿"));
                Assert.IsInstanceOf<RuleResult.GivePoints>(result);
                Assert.IsTrue(((RuleResult.GivePoints)result).Points > 150);
            }

            [Test]
            // TODO felk
            [Ignore("handling spaces was removed from the UnicodeCharacterCategoryRule and should be moved to its own rule")]
            public void detects_all_space_spam()
            {
                UnicodeCharacterCategoryRule rule = new(pointsPerBadChar: 2);
                RuleResult result = rule.Check(TextMessage(
                    "P A R T Y L I K E I T S T E N P M P A R T Y L I K E I T S T E N P M"));
                Assert.IsInstanceOf<RuleResult.GivePoints>(result);
                Assert.IsTrue(((RuleResult.GivePoints)result).Points > 50);
            }

            [Test]
            public void ignores_short_messages()
            {
                UnicodeCharacterCategoryRule rule = new();
                Assert.IsInstanceOf<RuleResult.Nothing>(rule.Check(TextMessage(
                    "♫ ┌༼ຈل͜ຈ༽┘ ♪ DANCE RIOT ♪ └༼ຈل͜ຈ༽┐♫")));
                Assert.IsInstanceOf<RuleResult.Nothing>(rule.Check(TextMessage(
                    "♫ └༼ຈل͜ຈ༽┐♫ O E A E A U I E O E A ♫ └༼ຈل͜ຈ༽┐ ♫")));
            }
        }

        [TestFixture]
        private class NewUserLink
        {
            [Test]
            public void detects_links_of_new_user()
            {
                var clockMock = Substitute.For<IClock>();
                NewUserLinkRule rule = new(clockMock);
                Message message = TextMessage("check this out https://i.imgur.com/ZinpOzD.png OhMyDog");
                Duration age = Duration.FromHours(2);
                clockMock.GetCurrentInstant().Returns(message.User.FirstActiveAt + age);

                RuleResult result = rule.Check(message);
                Assert.That(result, Is.InstanceOf<RuleResult.Timeout>());
                Assert.That(((RuleResult.Timeout)result).Message,
                    Is.EqualTo("account too new to TPP for posting links"));
            }

            [Test]
            public void tolerates_links_of_well_known_user()
            {
                var clockMock = Substitute.For<IClock>();
                NewUserLinkRule rule = new(clockMock);
                Message message = TextMessage("check this out https://i.imgur.com/ZinpOzD.png OhMyDog");
                Duration age = Duration.FromDays(50);
                clockMock.GetCurrentInstant().Returns(message.User.FirstActiveAt + age);

                Assert.That(rule.Check(message), Is.InstanceOf<RuleResult.Nothing>());
            }
        }

        [TestFixture]
        private class BannedWords
        {
            [Test]
            public void detects_banned_word()
            {
                BannedWordsRule rule = new(new[] { "penis" });
                Message message = TextMessage("𝓟énis haha");

                RuleResult result = rule.Check(message);
                Assert.That(result, Is.InstanceOf<RuleResult.Timeout>());
                Assert.That(((RuleResult.Timeout)result).Message,
                    Is.EqualTo("usage of banned word"));
            }

            [Test]
            public void ignored_not_banned_word()
            {
                BannedWordsRule rule = new(new[] { "penis" });
                Message message = TextMessage("boobs haha");
                Assert.That(rule.Check(message), Is.InstanceOf<RuleResult.Nothing>());
            }
        }
    }
}
