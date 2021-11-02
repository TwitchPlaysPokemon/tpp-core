using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests;

public class TransmutationCalculatorTest
{
    /// <summary>
    /// Performs a number of transmutations with a fixed-seed RNG and some semi-bogus rarities,
    /// just to make sure it doesn't crash and produces no obvious garbage data.
    /// Also this test might be able to detect subtle but unintended changes in behaviour.
    /// </summary>
    [Test]
    public void TestSomeTransmutationCalculations()
    {
        const int numTransmutations = 100;
        PkmnSpecies speciesCommon = PkmnSpecies.RegisterName("1", "common");
        PkmnSpecies speciesUncommon1 = PkmnSpecies.RegisterName("2", "uncommon1");
        PkmnSpecies speciesUncommon2 = PkmnSpecies.RegisterName("3", "uncommon2");
        PkmnSpecies speciesRare = PkmnSpecies.RegisterName("4", "rare");

        var badgeStatsRepoMock = new Mock<IBadgeStatsRepo>();
        var badgeStats = new Dictionary<PkmnSpecies, BadgeStat>
        {
            [speciesCommon] = new(speciesCommon, 10000, 10000, 10000, 10000, Rarity: 0.1),
            [speciesUncommon1] = new(speciesUncommon1, 100, 100, 100, 100, Rarity: 0.001),
            [speciesUncommon2] = new(speciesUncommon2, 100, 100, 100, 100, Rarity: 0.001),
            [speciesRare] = new(speciesRare, 1, 1, 1, 1, Rarity: 0.00001),
        }.ToImmutableSortedDictionary();
        badgeStatsRepoMock.Setup(bsr => bsr.GetBadgeStats()).ReturnsAsync(badgeStats);

        var random = new Random(numTransmutations);
        ITransmutationCalculator transmutationCalculator = new TransmutationCalculator(
            badgeStatsRepoMock.Object,
            badgeStats.Keys.ToImmutableSortedSet(),
            random: random.NextDouble
        );

        ImmutableSortedDictionary<PkmnSpecies, int> result = Enumerable
            .Range(0, numTransmutations)
            .Select(_ => transmutationCalculator
                .Transmute(ImmutableList.Create(speciesCommon, speciesCommon, speciesCommon)).Result)
            .GroupBy(species => species)
            .ToImmutableSortedDictionary(grp => grp.Key, grp => grp.Count());

        Assert.That(result.Keys, Is.EqualTo(new[] { speciesUncommon1, speciesUncommon2, speciesRare }));
        // these need to be adjusted when the RNG changes,
        // or the algorithm has changes in behaviour that are manually verified to be okay
        Assert.That(result[speciesUncommon1], Is.EqualTo(40));
        Assert.That(result[speciesUncommon2], Is.EqualTo(54));
        Assert.That(result[speciesRare], Is.EqualTo(6));
    }
}

public class TransmuterTest
{
    private static User MockUser(string name) => new(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0);

    [Test]
    public async Task TestSuccessfulTransmute()
    {
        User user = MockUser("user");
        PkmnSpecies speciesIn = PkmnSpecies.RegisterName("1", "SpeciesInput");
        PkmnSpecies speciesOut = PkmnSpecies.RegisterName("2", "SpeciesOutput");
        Instant instant = Instant.FromUnixTimeSeconds(123);

        var badgeRepoMock = new Mock<IBadgeRepo>();
        var transmutationCalculatorMock = new Mock<ITransmutationCalculator>();
        var bankMock = new Mock<IBank<User>>();
        var transmutationLogMock = new Mock<ITransmutationLogRepo>();
        var clockMock = new Mock<IClock>();

        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);
        IImmutableList<Badge> inputBadges = new List<Badge>
        {
            new("badgeIn1", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
            new("badgeIn2", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
            new("badgeIn3", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
        }.ToImmutableList();
        Badge outputBadge = new("badgeOut", user.Id, speciesOut, Badge.BadgeSource.Transmutation, Instant.MinValue);

        bankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(1);
        List<Dictionary<string, object?>> transferData = new();
        transmutationCalculatorMock.Setup(c => c.Transmute(inputSpeciesList)).ReturnsAsync(speciesOut);
        badgeRepoMock.Setup(r => r.FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count))
            .ReturnsAsync(inputBadges);
        badgeRepoMock.Setup(r => r.TransferBadges(inputBadges, null, "transmutation", Capture.In(transferData)))
            .ReturnsAsync(inputBadges);
        badgeRepoMock.Setup(r => r.AddBadge(user.Id, speciesOut, Badge.BadgeSource.Transmutation, null))
            .ReturnsAsync(outputBadge);
        List<Transaction<User>> transactionData = new();
        bankMock.Setup(b => b.PerformTransaction(Capture.In(transactionData), CancellationToken.None))
            .ReturnsAsync(default(TransactionLog)!); // result is not used, no need to mock a response
        clockMock.Setup(c => c.GetCurrentInstant()).Returns(instant);

        List<TransmuteEventArgs> transmuteEventArgsList = new();

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock.Object, transmutationCalculatorMock.Object, bankMock.Object,
            transmutationLogMock.Object, clockMock.Object);
        transmuter.Transmuted += (_, args) => transmuteEventArgsList.Add(args);
        Badge result = await transmuter.Transmute(user, 1, inputSpeciesList);

        Assert.That(result, Is.SameAs(outputBadge));
        Assert.That(transferData.Single(), Is.EqualTo(new Dictionary<string, object?>()));
        Assert.That(transactionData.Single().User, Is.EqualTo(user));
        Assert.That(transactionData.Single().Change, Is.EqualTo(-1));
        Assert.That(transactionData.Single().Type, Is.EqualTo("transmutation"));
        IImmutableList<string> inputBadgeIds = inputBadges.Select(b => b.Id).ToImmutableList();
        Assert.That(transactionData.Single().AdditionalData, Is.EqualTo(new Dictionary<string, object?>
        {
            ["input_badges"] = inputBadgeIds,
            ["output_badge"] = outputBadge.Id,
        }));
        Assert.That(transmuteEventArgsList.Single().User, Is.EqualTo(user));
        Assert.That(transmuteEventArgsList.Single().InputSpecies, Is.EqualTo(inputSpeciesList));
        Assert.That(transmuteEventArgsList.Single().OutputSpecies, Is.EqualTo(speciesOut));
        transmutationLogMock.Verify(l => l.Log(user.Id, instant, 1, inputBadgeIds, outputBadge.Id), Times.Once);
    }

    [Test]
    public void TestTransmuteBadgeNotOwned()
    {
        User user = MockUser("user");
        PkmnSpecies speciesIn = PkmnSpecies.RegisterName("1", "SpeciesInput");
        var badgeRepoMock = new Mock<IBadgeRepo>();
        var bankMock = new Mock<IBank<User>>();

        bankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(1);
        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);
        badgeRepoMock.Setup(r => r.FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count))
            .ReturnsAsync(ImmutableList<Badge>.Empty);

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock.Object, Mock.Of<ITransmutationCalculator>(), bankMock.Object,
            Mock.Of<ITransmutationLogRepo>(), Mock.Of<IClock>());
        var exception = Assert.ThrowsAsync<TransmuteException>(async () =>
            await transmuter.Transmute(user, 1, inputSpeciesList))!;
        Assert.That(exception.Message, Is.EqualTo("You don't have enough #001 SpeciesInput badges."));

        badgeRepoMock.Verify(r => r.FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count), Times.Once);
        badgeRepoMock.VerifyNoOtherCalls();
        bankMock.Verify(b => b.GetAvailableMoney(user), Times.Once);
        bankMock.VerifyNoOtherCalls();
    }

    [Test]
    public void TestTransmuteNotEnoughTokens()
    {
        User user = MockUser("user");
        PkmnSpecies speciesIn = PkmnSpecies.RegisterName("1", "SpeciesInput");
        var badgeRepoMock = new Mock<IBadgeRepo>();
        var bankMock = new Mock<IBank<User>>();

        bankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(0);
        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock.Object, Mock.Of<ITransmutationCalculator>(), bankMock.Object,
            Mock.Of<ITransmutationLogRepo>(), Mock.Of<IClock>());
        var exception = Assert.ThrowsAsync<TransmuteException>(async () =>
            await transmuter.Transmute(user, 1, inputSpeciesList))!;
        Assert.That(exception.Message, Is.EqualTo("You don't have the T1 required to transmute."));

        badgeRepoMock.VerifyNoOtherCalls();
        bankMock.Verify(b => b.GetAvailableMoney(user), Times.Once);
        bankMock.VerifyNoOtherCalls();
    }
}
