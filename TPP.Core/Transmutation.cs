using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Core;

public class TransmuteException : Exception
{
    public TransmuteException(string? message) : base(message) { }
}

public interface ITransmutationCalculator
{
    public const int TransmutationCost = 1;
    public const int MinTransmuteBadges = 3;

    Task<PkmnSpecies> Transmute(IImmutableList<PkmnSpecies> inputSpecies);
}

/// <summary>
/// Contains logic regarding transmutation calculations.
/// See the technical transmutation documentation at
/// <a href="https://twitchplayspokemon.tv/transmutation_calculations">twitchplayspokemon.tv/transmutation_calculations</a>
/// </summary>
public class TransmutationCalculator : ITransmutationCalculator
{
    private const double PiOver2 = Math.PI / 2d;
    private static readonly double Ln2 = Math.Log(2);

    private readonly IBadgeStatsRepo _badgeStatsRepo;
    private readonly Func<double> _random;
    private readonly IImmutableSet<PkmnSpecies> _transmutableSpecies;

    public TransmutationCalculator(
        IBadgeStatsRepo badgeStatsRepo,
        IImmutableSet<PkmnSpecies> transmutableSpecies,
        Func<double> random)
    {
        _badgeStatsRepo = badgeStatsRepo;
        _transmutableSpecies = transmutableSpecies;
        _random = random;
        if (!transmutableSpecies.Any())
            throw new ArgumentException("must provide at least 1 transmutable", nameof(transmutableSpecies));
    }

    private static double CombineRarities(IEnumerable<double> rarities) =>
        1d - Math.Atan(rarities.Select(x => Math.Tan(PiOver2 * (1d - x))).Sum()) / PiOver2;

    private double RandomizeRarity(double rarity, double total)
    {
        if (rarity == 0 || total == 0) return rarity;
        double x = _random();
        double product = rarity * total;
        double productlog = Math.Log(product);
        double parameter = (Math.Log(1 + product) + productlog - Ln2) / (rarity * productlog);
        if (x <= 0.5d)
        {
            return Math.Pow(rarity * (2 * x), 1d / (rarity * parameter - 1d));
        }
        else
        {
            double invRarity = 1d - rarity;
            return 1d - Math.Pow(invRarity * (2d - 2d * x), 1d / (invRarity * parameter - 1d));
        }
    }

    private double GetTransmutedRarity(IEnumerable<double> rarities, double total)
    {
        double combined = CombineRarities(rarities);
        double randomized = RandomizeRarity(combined, total);
        return Math.Clamp(randomized, 0d, 1d);
    }

    private PkmnSpecies GetBadgeForRarity(double rarity, IDictionary<PkmnSpecies, double> rarities)
    {
        ((PkmnSpecies, double)? closestNeg, (PkmnSpecies, double)? closestPos) = (null, null);

        foreach ((PkmnSpecies species, double r) in rarities.OrderBy(kvp => (kvp.Value, _random())))
        {
            if (r < rarity)
            {
                closestNeg = (species, r);
            }
            else
            {
                closestPos = (species, r);
                break;
            }
        }
        if (closestNeg == null || closestPos == null)
            return closestNeg?.Item1 ?? closestPos?.Item1
                ?? throw new ArgumentException("must provide at least 1 possible result rarity", nameof(rarities));
        double min = closestNeg.Value.Item2;
        double max = closestPos.Value.Item2;
        double randomRounding = min + _random() * (max - min);
        return randomRounding < rarity ? closestNeg.Value.Item1 : closestPos.Value.Item1;
    }

    public async Task<PkmnSpecies> Transmute(IImmutableList<PkmnSpecies> inputSpecies)
    {
        if (inputSpecies.Count < ITransmutationCalculator.MinTransmuteBadges)
            throw new TransmuteException(
                $"Must transmute at least {ITransmutationCalculator.MinTransmuteBadges} badges");
        IDictionary<PkmnSpecies, BadgeStat> stats = await _badgeStatsRepo.GetBadgeStats();
        IImmutableSet<PkmnSpecies> transmutables = _transmutableSpecies
            .Except(inputSpecies)
            .Where(t => stats.ContainsKey(t) && stats[t].Rarity > 0)
            .ToImmutableHashSet();
        if (!transmutables.Any())
        {
            throw new TransmuteException(
                "there are no transmutables left after removing all input species from the pool");
        }
        HashSet<PkmnSpecies> illegalInputs = inputSpecies.Except(_transmutableSpecies).ToHashSet();
        if (illegalInputs.Any())
            throw new TransmuteException(string.Join(", ", illegalInputs) + " cannot be used for transmutation");

        double totalExisting = transmutables.Select(t => stats[t].RarityCount).Sum();
        double totalGenerated = transmutables.Select(t => stats[t].RarityCountGenerated).Sum();
        ImmutableSortedDictionary<PkmnSpecies, double> rarities =
            transmutables.ToImmutableSortedDictionary(t => t, t => stats[t].Rarity);

        const double factor = BadgeRepo.CountExistingFactor;
        double total = 1d / ((1d - factor) / totalGenerated + factor / totalExisting);

        double transmutedRarity = GetTransmutedRarity(inputSpecies.Select(s => stats[s].Rarity), total);
        return GetBadgeForRarity(transmutedRarity, rarities);
    }
}

public class TransmuteEventArgs : EventArgs
{
    public User User { get; }
    public IImmutableList<PkmnSpecies> InputSpecies { get; }
    public PkmnSpecies OutputSpecies { get; }
    public IImmutableList<PkmnSpecies> Candidates { get; }

    public TransmuteEventArgs(
        User user,
        IImmutableList<PkmnSpecies> inputSpecies,
        PkmnSpecies outputSpecies,
        IImmutableList<PkmnSpecies> candidates)
    {
        User = user;
        InputSpecies = inputSpecies;
        OutputSpecies = outputSpecies;
        Candidates = candidates;
    }
}

public interface ITransmuter
{
    Task<Badge> Transmute(User user, int tokens, IImmutableList<PkmnSpecies> speciesList);

    event EventHandler<TransmuteEventArgs> Transmuted;
}

/// <summary>
/// Performs actual transmutation: removing the consumed badges, creating the new badge, deducting tokens etc.
/// </summary>
public class Transmuter : ITransmuter
{
    private static readonly Random Random = new();

    private readonly IBadgeRepo _badgeRepo;
    private readonly ITransmutationCalculator _transmutationCalculator;
    private readonly IBank<User> _tokenBank;
    private readonly ITransmutationLogRepo _transmutationLogRepo;
    private readonly IClock _clock;

    public event EventHandler<TransmuteEventArgs>? Transmuted;

    public Transmuter(
        IBadgeRepo badgeRepo,
        ITransmutationCalculator transmutationCalculator,
        IBank<User> tokenBank,
        ITransmutationLogRepo transmutationLogRepo,
        IClock clock)
    {
        _badgeRepo = badgeRepo;
        _transmutationCalculator = transmutationCalculator;
        _tokenBank = tokenBank;
        _transmutationLogRepo = transmutationLogRepo;
        _clock = clock;
    }

    public async Task<Badge> Transmute(User user, int tokens, IImmutableList<PkmnSpecies> speciesList)
    {
        if (tokens != ITransmutationCalculator.TransmutationCost)
            throw new TransmuteException(
                $"Must pay exactly {ITransmutationCalculator.TransmutationCost} token to transmute.");
        if (speciesList.Count < ITransmutationCalculator.MinTransmuteBadges)
            throw new TransmuteException(
                $"Must transmute at least {ITransmutationCalculator.MinTransmuteBadges} badges.");
        if (await _tokenBank.GetAvailableMoney(user) < tokens)
            throw new TransmuteException(
                $"You don't have the T{ITransmutationCalculator.TransmutationCost} required to transmute.");

        IImmutableList<Badge> inputBadges = ImmutableList<Badge>.Empty;
        foreach (var grouping in speciesList.GroupBy(s => s))
        {
            PkmnSpecies species = grouping.Key;
            int numRequired = grouping.Count();
            IImmutableList<Badge> badges = await _badgeRepo
                .FindByUserAndSpecies(user.Id, species, limit: numRequired);
            if (badges.Count < numRequired)
                throw new TransmuteException($"You don't have enough {species} badges.");
            inputBadges = inputBadges.Concat(badges).ToImmutableList();
        }

        PkmnSpecies resultSpecies = await _transmutationCalculator.Transmute(speciesList);

        Dictionary<string, object?> additionalData = new();
        IImmutableList<Badge> consumedBadges = await _badgeRepo
            .TransferBadges(inputBadges, recipientUserId: null, BadgeLogType.Transmutation, additionalData);
        Badge resultBadge = await _badgeRepo.AddBadge(user.Id, resultSpecies, Badge.BadgeSource.Transmutation);

        IImmutableList<string> inputIds = consumedBadges.Select(b => b.Id).ToImmutableList();
        await _tokenBank.PerformTransaction(new Transaction<User>(user, -tokens,
            TransactionType.Transmutation, new Dictionary<string, object?>
            {
                ["input_badges"] = inputIds,
                ["output_badge"] = resultBadge.Id
            }));
        await _transmutationLogRepo.Log(user.Id, _clock.GetCurrentInstant(), tokens, inputIds, resultBadge.Id);

        await OnTransmuted(user, speciesList, resultSpecies);
        return resultBadge;
    }

    private async Task OnTransmuted(User user, IImmutableList<PkmnSpecies> inputs, PkmnSpecies output)
    {
        List<PkmnSpecies> candidates = new();
        for (int i = 0; i < 5; i++)
            candidates.Add(await _transmutationCalculator.Transmute(inputs));
        candidates.Insert(Random.Next(0, candidates.Count), output);
        var args = new TransmuteEventArgs(user, inputs, output, candidates.ToImmutableList());
        Transmuted?.Invoke(this, args);
    }
}
