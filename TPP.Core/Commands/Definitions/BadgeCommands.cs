using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions
{
    /// <summary>
    /// Helper class to store all relevant Information for a single Region
    /// </summary>
    public record RegionInformation(Generation Generation, int TotalRegionCount);

    public class BadgeCommands : ICommandCollection
    {
        // using an enum didn't work out, because no "-" characters allowed in enums. So go with const strings instead, or change modes to camel-case
        private const string PokedexModeComplementFrom = "complement-from";
        private const string PokedexModeComplementFromDupes = "complement-from-dupes";
        private const string PokedexModeMissing = "missing";
        private const string PokedexModeDupes = "dupes";
        private const string PokedexModeModes = "modes";
        private const string PokedexModeNational = "national";

        public IEnumerable<Command> Commands => new[]
        {
            new Command("badges", Badges)
            {
                Aliases = new[] { "badge" },
                Description = "Show a user's badges. Argument: <Pokemon> (optional) <Username> (optional)"
            }.WithPerUserCooldown(Duration.FromSeconds(3)),

            new Command("unselectbadge", UnselectBadge)
            {
                Aliases = new[] { "unchoosebadge", "unequipbadge" },
                Description = "Unequip your displayed badge."
            },

            new Command("selectbadge", SelectBadge)
            {
                Aliases = new[] { "choosebadge", "equipbadge" },
                Description = "Change your displayed badge. Argument: <Pokemon>"
            },

            new Command("checkbadge", CheckBadge)
            {
                Description = "View statistics about a badge. Argument: <Pokemon>"
            },

            new Command("pokedex", Pokedex)
            {
                Aliases = new[] { "dex" },
                Description = "Show how many different species of badge a user owns. Argument: <username> (optional) <mode> (optional). For more info, type \"!dex modes\""
            }.WithPerUserCooldown(Duration.FromSeconds(3)),

            new Command("giftbadge", GiftBadge)
            {
                Description =
                    "Gift a badge you own to another user with no price. Arguments: <pokemon> <number of badges>(Optional) <username>"
            },
        };

        private readonly IBadgeRepo _badgeRepo;
        private readonly IBadgeStatsRepo _badgeStatsRepo;
        private readonly IUserRepo _userRepo;
        private readonly IMessageSender _messageSender;
        private readonly IImmutableSet<PkmnSpecies> _knownSpecies;
        private readonly Dictionary<string, RegionInformation> _pokedexModeRegions;

        public BadgeCommands(
            IBadgeRepo badgeRepo,
            IBadgeStatsRepo badgeStatsRepo,
            IUserRepo userRepo,
            IMessageSender messageSender,
            IImmutableSet<PkmnSpecies> knownSpecies
        )
        {
            _badgeRepo = badgeRepo;
            _badgeStatsRepo = badgeStatsRepo;
            _userRepo = userRepo;
            _messageSender = messageSender;
            _knownSpecies = knownSpecies;
            _pokedexModeRegions = new Dictionary<string, RegionInformation>
            {
                { "kanto", new RegionInformation(Generation.Gen1, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen1))},
                { "johto", new RegionInformation(Generation.Gen2, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen2))},
                { "hoenn", new RegionInformation(Generation.Gen3, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen3))},
                { "sinnoh", new RegionInformation(Generation.Gen4, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen4))},
                { "unova", new RegionInformation(Generation.Gen5, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen5))},
                { "kalos", new RegionInformation(Generation.Gen6, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen6))},
                { "alola", new RegionInformation(Generation.Gen7, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen7))},
                { "galar", new RegionInformation(Generation.Gen8, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.Gen8))},
                { "fakemons", new RegionInformation(Generation.GenFake, _knownSpecies.Count(pokemon => pokemon.GetGeneration() == Generation.GenFake))},
                { PokedexModeNational, new RegionInformation(Generation.GenFake, _knownSpecies.Count(pokemon => pokemon.GetGeneration() != Generation.GenFake))}
            };
        }

        public async Task<CommandResult> Badges(CommandContext context)
        {
            (Optional<PkmnSpecies> optionalSpecies, Optional<User> optionalUser) =
                await context.ParseArgs<AnyOrder<Optional<PkmnSpecies>, Optional<User>>>();
            bool isSelf = !optionalUser.IsPresent;
            User user = isSelf ? context.Message.User : optionalUser.Value;
            if (optionalSpecies.IsPresent)
            {
                PkmnSpecies species = optionalSpecies.Value;
                long numBadges = await _badgeRepo.CountByUserAndSpecies(user.Id, species);
                return new CommandResult
                {
                    Response = numBadges == 0
                        ? isSelf
                            ? $"You have no {species} badges."
                            : $"{user.Name} has no {species} badges."
                        : isSelf
                            ? $"You have {numBadges}x {species} badges."
                            : $"{user.Name} has {numBadges}x {species} badges."
                };
            }
            else
            {
                ImmutableSortedDictionary<PkmnSpecies, int> numBadgesPerSpecies =
                    await _badgeRepo.CountByUserPerSpecies(user.Id);
                if (!numBadgesPerSpecies.Any())
                {
                    return new CommandResult
                    {
                        Response = isSelf ? "You have no badges." : $"{user.Name} has no badges."
                    };
                }
                if (numBadgesPerSpecies.Count > 20)
                {
                    return new CommandResult
                    {
                        Response = $"Please see https://twitchplayspokemon.tv/badges_for_user/{user.SimpleName}"
                    };
                }
                IEnumerable<string> badgesFormatted = numBadgesPerSpecies.Select(kvp => $"{kvp.Value}x {kvp.Key}");
                return new CommandResult
                {
                    Response = isSelf
                        ? $"Your badges: {string.Join(", ", badgesFormatted)}"
                        : $"{user.Name}'s badges: {string.Join(", ", badgesFormatted)}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
        }

        public async Task<CommandResult> UnselectBadge(CommandContext context)
        {
            if (context.Message.User.SelectedBadge == null)
            {
                return new CommandResult { Response = "You don't have a badge equipped." };
            }
            PkmnSpecies? badge = context.Message.User.SelectedBadge;
            await _userRepo.SetSelectedBadge(context.Message.User, null);
            return new CommandResult { Response = $"{badge} badge unequipped." };
        }

        public async Task<CommandResult> SelectBadge(CommandContext context)
        {
            var species = await context.ParseArgs<PkmnSpecies>();
            bool isOwned = await _badgeRepo.HasUserBadge(context.Message.User.Id, species);
            if (!isOwned)
            {
                return new CommandResult { Response = $"{species} is not an owned badge." };
            }
            await _userRepo.SetSelectedBadge(context.Message.User, species);
            return new CommandResult { Response = $"{species} selected as badge." };
        }

        public async Task<CommandResult> CheckBadge(CommandContext context)
        {
            var species = await context.ParseArgs<PkmnSpecies>();
            BadgeStat? stats = await _badgeStatsRepo.GetBadgeStatsForSpecies(species);

            int num = stats?.Count ?? 0;
            double rarity = stats?.Rarity ?? 0;
            string logInvRarityStr = rarity == 0
                ? "infinity"
                : Math.Log(1d / rarity).ToString("F1", CultureInfo.InvariantCulture);
            string message = $"{species} badges existing: {num}, " +
                             $"logarithmic inverse rarity: {logInvRarityStr}, higher is better";
            if (num <= 10)
            {
                IImmutableList<string> ownerIds = await _badgeRepo.FindOwnerIdsForSpecies(species);
                IEnumerable<string> ownerNames = await Task.WhenAll(
                    ownerIds.Select(async userId => (await _userRepo.FindById(userId))?.Name ?? "<unknown>"));
                message += ", owned by " + string.Join(", ", ownerNames);
            }
            return new CommandResult { Response = message };
        }

        public async Task<CommandResult> Pokedex(CommandContext context)
        {
            (Optional<User> optionalUser, Optional<string> optionalMode, Optional<User> optionalCompareUser) =
                await context.ParseArgs<Optional<User>, Optional<string>, Optional<User>>();
            User user = optionalUser.OrElse(context.Message.User);
            bool isSelf = user == context.Message.User;

            if (!optionalMode.IsPresent)
            {
                int numUniqueSpecies = (await _badgeRepo.CountByUserPerSpecies(user.Id)).Count;
                return new CommandResult
                {
                    Response = isSelf
                        ? $"You have collected {numUniqueSpecies} distinct Pokémon badge(s)"
                        : $"{user.Name} has collected {numUniqueSpecies} distinct Pokémon badge(s)"
                };
            }

            string mode = optionalMode.Value.ToLower();
            ImmutableSortedDictionary<PkmnSpecies, int> numBadgesPerSpecies =
                await _badgeRepo.CountByUserPerSpecies(user.Id);

            if (mode.Equals(PokedexModeMissing))
            {
                IEnumerable<PkmnSpecies> missingList = _knownSpecies.Except(numBadgesPerSpecies.Keys);
                IEnumerable<string> badgesFormatted = missingList.Select(entry => $"{entry}");
                return new CommandResult
                {
                    Response = isSelf
                        ? $"You are currently missing the following badge(s): {string.Join(", ", badgesFormatted)}"
                        : $"{user.Name} is currently missing the following badge(s): {string.Join(", ", badgesFormatted)}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
            else if (mode.Equals(PokedexModeDupes))
            {
                var dupes = numBadgesPerSpecies.Where(kvp => kvp.Value > 1)
                    .ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value);
                if (!dupes.Any())
                {
                    return new CommandResult
                    {
                        Response = isSelf
                            ? "You do not own any duplicate Pokémon badges"
                            : $"{user.Name} does not own any duplicate Pokémon badges"
                    };
                }
                IEnumerable<string> badgesFormatted = dupes.Select(kvp => $"{kvp.Value - 1}x {kvp.Key}");
                return new CommandResult
                {
                    Response = isSelf
                        ? $"You own the following duplicate badge(s): {string.Join(", ", badgesFormatted)}"
                        : $"{user.Name} owns the following duplicate badge(s): {string.Join(", ", badgesFormatted)}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
            else if (mode.Equals(PokedexModeComplementFromDupes))
            {
                if (!optionalCompareUser.IsPresent)
                {
                    return new CommandResult
                    {
                        Response = $"Mode {PokedexModeComplementFromDupes} requires a second user argument"
                    };
                }
                ImmutableSortedDictionary<PkmnSpecies, int> compareUser =
                    await _badgeRepo.CountByUserPerSpecies(optionalCompareUser.Value.Id);
                var compareUserDupeList = compareUser.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key);

                var differenceList = compareUserDupeList.Except(numBadgesPerSpecies.Keys).ToList();
                if (!differenceList.Any())
                {
                    return new CommandResult
                    {
                        Response = $"{optionalCompareUser.Value.Name} does not own any duplicate Pokémon badges {user.Name} is missing"
                    };
                }
                IEnumerable<string> badgesFormatted = differenceList.Select(entry => $"{entry}");
                return new CommandResult
                {
                    Response = $"{optionalCompareUser.Value.Name} owns the following duplicate badge(s) {user.Name} is missing: {string.Join(", ", badgesFormatted)}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
            else if (mode.Equals(PokedexModeComplementFrom))
            {
                if (!optionalCompareUser.IsPresent)
                {
                    return new CommandResult
                    {
                        Response = $"Mode {PokedexModeComplementFrom} requires a second user argument"
                    };
                }
                ImmutableSortedDictionary<PkmnSpecies, int> compareUser =
                    await _badgeRepo.CountByUserPerSpecies(optionalCompareUser.Value.Id);

                var differenceList = compareUser.Keys.Except(numBadgesPerSpecies.Keys).ToList();
                if (!differenceList.Any())
                {
                    return new CommandResult
                    {
                        Response = $"{optionalCompareUser.Value.Name} does not own any Pokémon badges {user.Name} is missing"
                    };
                }
                IEnumerable<string> badgesFormatted = differenceList.Select(entry => $"{entry}");
                return new CommandResult
                {
                    Response = $"{optionalCompareUser.Value.Name} owns the following badge(s) {user.Name} is missing: {string.Join(", ", badgesFormatted)}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
            else if (_pokedexModeRegions.ContainsKey(mode))
            {
                RegionInformation regionInformation = _pokedexModeRegions[mode];
                ImmutableSortedDictionary<PkmnSpecies, int> ownedPokemons = await _badgeRepo.CountByUserPerSpecies(user.Id);
                int userOwnedRegionCount = mode.Equals(PokedexModeNational)
                    ? ownedPokemons.Count(ownedPokemon => ownedPokemon.Key.GetGeneration() != regionInformation.Generation)
                    : ownedPokemons.Count(ownedPokemon => ownedPokemon.Key.GetGeneration() == regionInformation.Generation);
                return new CommandResult
                {
                    Response = isSelf
                        ? $"You have collected {userOwnedRegionCount}/{regionInformation.TotalRegionCount} "
                          + $"distinct {mode[0].ToString().ToUpper() + mode[1..].ToLower()} badge(s)"
                        : $"{user.Name} has collected {userOwnedRegionCount}/{regionInformation.TotalRegionCount} "
                          + $"distinct {mode[0].ToString().ToUpper() + mode[1..].ToLower()} badge(s)"
                };
            }
            else if (mode.Equals(PokedexModeModes))
            {
                return new CommandResult
                {
                    Response = $"Supported modes are '{PokedexModeDupes}': Show duplicate badges, '{PokedexModeMissing}': Show missing badges, "
                        + $"'{PokedexModeComplementFrom}' Compares missing badges from User A with owned badges from User B, "
                        + $"'{PokedexModeComplementFromDupes}' Compares missing badges from User A with owned duplicate badges from User B, "
                        + "'kanto', 'johto,',... Shows how many badges you or the specified user owns from the specified region only"
                };
            }
            return new CommandResult
            {
                Response = $"Unsupported mode '{mode}'. Current modes supported: {PokedexModeModes}, {PokedexModeDupes}, {PokedexModeMissing}, {PokedexModeComplementFromDupes}, "
                    + $"{PokedexModeComplementFrom}, kanto, johto,... "
            };
        }

        public async Task<CommandResult> GiftBadge(CommandContext context)
        {
            User gifter = context.Message.User;
            (User recipient, PkmnSpecies species, Optional<PositiveInt> amountOpt) =
                await context.ParseArgs<AnyOrder<User, PkmnSpecies, Optional<PositiveInt>>>();
            int amount = amountOpt.Map(i => i.Number).OrElse(1);

            if (recipient == gifter)
                return new CommandResult { Response = "You cannot gift to yourself" };
            if (recipient.Banned)
                return new CommandResult { Response = "You cannot gift to a banned user." };

            IImmutableList<Badge> badges = await _badgeRepo.FindByUserAndSpecies(gifter.Id, species, amount);
            if (badges.Count < amount)
                return new CommandResult
                {
                    Response = $"You tried to gift {amount} {species} badges, but you only have {badges.Count}."
                };

            IImmutableList<Badge> badgesToGift = badges.Take(amount).ToImmutableList();
            var data = new Dictionary<string, object?> { ["gifter"] = gifter.Id };
            await _badgeRepo.TransferBadges(badgesToGift, recipient.Id, BadgeLogType.TransferGift, data);

            await _messageSender.SendWhisper(recipient, amount > 1
                ? $"You have been gifted {amount} {species} badges from {gifter.Name}!"
                : $"You have been gifted a {species} badge from {gifter.Name}!");
            await _messageSender.SendMessage(amount > 1
                ? $"{gifter.Name} has gifted {amount} {species} badges to {recipient.Name}!"
                : $"{gifter.Name} has gifted a {species} badge to {recipient.Name}!");
            return new CommandResult
            {
                Response = amount > 1
                    ? $"You successfully gifted {amount} {species} badges to {recipient.Name}!"
                    : $"You successfully gifted a {species} badge to {recipient.Name}!",
                ResponseTarget = ResponseTarget.NoneIfChat
            };
        }
    }
}
