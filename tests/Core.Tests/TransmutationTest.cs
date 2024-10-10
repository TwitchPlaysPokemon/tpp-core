using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using Common;
using Model;
using Persistence;

namespace Core.Tests;

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

        var badgeStatsRepoMock = Substitute.For<IBadgeStatsRepo>();
        var badgeStats = new Dictionary<PkmnSpecies, BadgeStat>
        {
            [speciesCommon] = new(speciesCommon, 10000, 10000, 10000, 10000, Rarity: 0.1),
            [speciesUncommon1] = new(speciesUncommon1, 100, 100, 100, 100, Rarity: 0.001),
            [speciesUncommon2] = new(speciesUncommon2, 100, 100, 100, 100, Rarity: 0.001),
            [speciesRare] = new(speciesRare, 1, 1, 1, 1, Rarity: 0.00001),
        }.ToImmutableSortedDictionary();
        badgeStatsRepoMock.GetBadgeStats().Returns(badgeStats);

        var random = new Random(numTransmutations);
        ITransmutationCalculator transmutationCalculator = new TransmutationCalculator(
            badgeStatsRepoMock,
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

    [Test]
    public void TestCannotTransmuteNonExistingBadge()
    {
        const int numTransmutations = 100;
        PkmnSpecies speciesUncommon = PkmnSpecies.RegisterName("1", "uncommon");
        PkmnSpecies speciesRare = PkmnSpecies.RegisterName("2", "rare");
        PkmnSpecies speciesNonExisting = PkmnSpecies.RegisterName("3", "non-existing");

        var badgeStatsRepoMock = Substitute.For<IBadgeStatsRepo>();
        var badgeStats = new Dictionary<PkmnSpecies, BadgeStat>
        {
            [speciesUncommon] = new(speciesUncommon, 100, 100, 100, 100, Rarity: 0.001),
            [speciesRare] = new(speciesRare, 1, 1, 1, 1, Rarity: 0.00001),
            [speciesNonExisting] = new(speciesRare, 0, 0, 0, 0, Rarity: 0),
        }.ToImmutableSortedDictionary();
        badgeStatsRepoMock.GetBadgeStats().Returns(badgeStats);

        var random = new Random(numTransmutations);
        ITransmutationCalculator transmutationCalculator = new TransmutationCalculator(
            badgeStatsRepoMock,
            badgeStats.Keys.ToImmutableSortedSet(),
            random: random.NextDouble
        );

        ImmutableSortedDictionary<PkmnSpecies, int> result = Enumerable
            .Range(0, numTransmutations)
            .Select(_ => transmutationCalculator
                .Transmute(Enumerable.Repeat(speciesUncommon, 100).ToImmutableList()).Result)
            .GroupBy(species => species)
            .ToImmutableSortedDictionary(grp => grp.Key, grp => grp.Count());

        Assert.That(result.Keys, Is.EqualTo(new[] { speciesRare }));
        // these need to be adjusted when the RNG changes,
        // or the algorithm has changes in behaviour that are manually verified to be okay
        Assert.That(result[speciesRare], Is.EqualTo(numTransmutations));
        Assert.That(result, Does.Not.ContainKey(speciesNonExisting));
    }

    /// <summary>
    /// If a badge is not a possible transmutation result,
    /// it should also be rejected as a transmutation input.
    /// </summary>
    [Test]
    public void TestCannotTransmuteUntransmutableBadge()
    {
        PkmnSpecies speciesTransmutable = PkmnSpecies.RegisterName("1", "yes");
        PkmnSpecies speciesUntransmutable = PkmnSpecies.RegisterName("2", "no");

        var badgeStatsRepoMock = Substitute.For<IBadgeStatsRepo>();
        var badgeStats = new Dictionary<PkmnSpecies, BadgeStat>
        {
            [speciesTransmutable] = new(speciesTransmutable, 100, 100, 100, 100, Rarity: 0.001),
        }.ToImmutableSortedDictionary();
        badgeStatsRepoMock.GetBadgeStats().Returns(badgeStats);

        ITransmutationCalculator transmutationCalculator = new TransmutationCalculator(
            badgeStatsRepoMock,
            ImmutableHashSet.Create(speciesTransmutable),
            random: () => 12345
        );

        TransmuteException exception = Assert.ThrowsAsync<TransmuteException>(async () => await transmutationCalculator
            .Transmute(ImmutableList.Create(speciesUntransmutable, speciesUntransmutable, speciesUntransmutable)))!;
        Assert.That(exception.Message, Is.EqualTo(speciesUntransmutable + " cannot be used for transmutation"));
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

        var badgeRepoMock = Substitute.For<IBadgeRepo>();
        var transmutationCalculatorMock = Substitute.For<ITransmutationCalculator>();
        var bankMock = Substitute.For<IBank<User>>();
        var transmutationLogMock = Substitute.For<ITransmutationLogRepo>();
        var clockMock = Substitute.For<IClock>();

        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);
        IImmutableList<Badge> inputBadges = new List<Badge>
        {
            new("badgeIn1", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
            new("badgeIn2", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
            new("badgeIn3", user.Id, speciesIn, Badge.BadgeSource.Pinball, Instant.MinValue),
        }.ToImmutableList();
        Badge outputBadge = new("badgeOut", user.Id, speciesOut, Badge.BadgeSource.Transmutation, Instant.MinValue);

        bankMock.GetAvailableMoney(user).Returns(1);
        List<Dictionary<string, object?>> transferData = new();
        transmutationCalculatorMock.Transmute(inputSpeciesList).Returns(speciesOut);
        badgeRepoMock.FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count)
            .Returns(inputBadges);
        badgeRepoMock.TransferBadges(inputBadges, null, "transmutation",
                Arg.Do<Dictionary<string, object?>>(td => transferData.Add(td)))
            .ReturnsForAnyArgs(inputBadges);
        badgeRepoMock.AddBadge(user.Id, speciesOut, Badge.BadgeSource.Transmutation, null)
            .Returns(outputBadge);
        List<Transaction<User>> transactionData = new();
        bankMock.PerformTransaction(
                Arg.Do<Transaction<User>>(u => transactionData.Add(u)), CancellationToken.None)
            .Returns(default(TransactionLog)!); // result is not used, no need to mock a response
        clockMock.GetCurrentInstant().Returns(instant);

        List<TransmuteEventArgs> transmuteEventArgsList = new();

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock, transmutationCalculatorMock, bankMock,
            transmutationLogMock, clockMock);
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
        await transmutationLogMock.Received(1).Log(user.Id, instant, 1,
            Arg.Is<IImmutableList<string>>(lst => lst.SequenceEqual(inputBadgeIds)), outputBadge.Id);
    }

    [Test]
    public async Task TestTransmuteBadgeNotOwned()
    {
        User user = MockUser("user");
        PkmnSpecies speciesIn = PkmnSpecies.RegisterName("1", "SpeciesInput");
        var badgeRepoMock = Substitute.For<IBadgeRepo>();
        var bankMock = Substitute.For<IBank<User>>();

        bankMock.GetAvailableMoney(user).Returns(1);
        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);
        badgeRepoMock.FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count)
            .Returns(ImmutableList<Badge>.Empty);

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock, Substitute.For<ITransmutationCalculator>(), bankMock,
            Substitute.For<ITransmutationLogRepo>(), Substitute.For<IClock>());
        var exception = Assert.ThrowsAsync<TransmuteException>(async () =>
            await transmuter.Transmute(user, 1, inputSpeciesList))!;
        Assert.That(exception.Message, Is.EqualTo("You don't have enough #001 SpeciesInput badges."));

        await badgeRepoMock.Received(1).FindByUserAndSpecies(user.Id, speciesIn, inputSpeciesList.Count);
        Assert.That(badgeRepoMock.ReceivedCalls().Count(), Is.EqualTo(1));
        await bankMock.Received(1).GetAvailableMoney(user);
        Assert.That(bankMock.ReceivedCalls().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task TestTransmuteNotEnoughTokens()
    {
        User user = MockUser("user");
        PkmnSpecies speciesIn = PkmnSpecies.RegisterName("1", "SpeciesInput");
        var badgeRepoMock = Substitute.For<IBadgeRepo>();
        var bankMock = Substitute.For<IBank<User>>();

        bankMock.GetAvailableMoney(user).Returns(0);
        ImmutableList<PkmnSpecies> inputSpeciesList = ImmutableList.Create(speciesIn, speciesIn, speciesIn);

        ITransmuter transmuter = new Transmuter(
            badgeRepoMock, Substitute.For<ITransmutationCalculator>(), bankMock,
            Substitute.For<ITransmutationLogRepo>(), Substitute.For<IClock>());
        var exception = Assert.ThrowsAsync<TransmuteException>(async () =>
            await transmuter.Transmute(user, 1, inputSpeciesList))!;
        Assert.That(exception.Message, Is.EqualTo("You don't have the T1 required to transmute."));

        Assert.That(badgeRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
        await bankMock.Received(1).GetAvailableMoney(user);
        Assert.That(bankMock.ReceivedCalls().Count(), Is.EqualTo(1));
    }
}
