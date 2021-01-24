using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Commands.Definitions
{
    public class BadgeCommands : ICommandCollection
    {
        public IEnumerable<Command> Commands => new[]
        {
            new Command("badges", Badges)
            {
                Aliases = new[] { "badge" },
                Description = "Show a user's badges. Argument: <Pokemon> (optional) <Username> (optional)"
            },

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

            new Command("pokedex", Pokedex)
            {
                Aliases = new[] { "dex" },
                Description = "Show how many different species of badge a user owns. Argument: <username> (optional)"
            },
        };

        private readonly IBadgeRepo _badgeRepo;
        private readonly IUserRepo _userRepo;
        private readonly HashSet<PkmnSpecies>? _whitelist;

        public BadgeCommands(IBadgeRepo badgeRepo, IUserRepo userRepo, HashSet<PkmnSpecies>? whitelist = null)
        {
            _badgeRepo = badgeRepo;
            _userRepo = userRepo;
            _whitelist = whitelist;
        }

        public async Task<CommandResult> Badges(CommandContext context)
        {
            (Optional<PkmnSpecies> optionalSpecies, Optional<User> optionalUser) =
                await context.ParseArgs<Optional<PkmnSpecies>, Optional<User>>();
            Console.WriteLine($"species present: {optionalSpecies.IsPresent}");
            Console.WriteLine($"user present: {optionalUser.IsPresent}");
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
            if (_whitelist != null && !_whitelist.Contains(species))
            {
                return new CommandResult { Response = $"Oi mate, you got a loicense for that there {species}?" };
            }
            await _userRepo.SetSelectedBadge(context.Message.User, species);
            return new CommandResult { Response = $"{species} selected as badge." };
        }

        public async Task<CommandResult> Pokedex(CommandContext context)
        {
            User user = (await context.ParseArgs<Optional<User>>()).OrElse(context.Message.User);
            bool isSelf = user == context.Message.User;
            int numUniqueSpecies = (await _badgeRepo.CountByUserPerSpecies(user.Id)).Count;
            return new CommandResult
            {
                Response = isSelf
                    ? $"You have collected {numUniqueSpecies} distinct Pokémon badge(s)"
                    : $"{user} has collected {numUniqueSpecies} distinct Pokémon badge(s)"
            };
        }
    }
}
