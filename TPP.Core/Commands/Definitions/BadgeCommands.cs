using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Core.Chat;
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
                Description = "Show how many different species of badge a user owns. Argument: <username> (optional) <mode> (optional). Supported modes are dupes,missing"
            },

            new Command("giftbadge", GiftBadge)
            {
                Description =
                    "Gift a badge you own to another user with no price. Arguments: <pokemon> <number of badges>(Optional) <username>"
            },
        };

        private readonly IBadgeRepo _badgeRepo;
        private readonly IUserRepo _userRepo;
        private readonly IMessageSender _messageSender;
        private readonly HashSet<PkmnSpecies>? _whitelist;

        public BadgeCommands(
            IBadgeRepo badgeRepo,
            IUserRepo userRepo,
            IMessageSender messageSender,
            HashSet<PkmnSpecies>? whitelist = null)
        {
            _badgeRepo = badgeRepo;
            _userRepo = userRepo;
            _messageSender = messageSender;
            _whitelist = whitelist;
        }

        public async Task<CommandResult> Badges(CommandContext context)
        {
            (Optional<PkmnSpecies> optionalSpecies, Optional<User> optionalUser) =
                await context.ParseArgs<Optional<PkmnSpecies>, Optional<User>>();
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
            (Optional<User> optionalUser, Optional<string> mode_) =
                await context.ParseArgs<Optional<User>, Optional<string>>();
            User user = optionalUser.IsPresent ? optionalUser.Value : context.Message.User;
            bool isSelf = user == context.Message.User;

            // Have to additionally check if the string is not empty, otherwise "!dex " would jump right into the else - branch.
            //   Not sure if this is an actual issue when connected to twitch
            if( !mode_.IsPresent || string.IsNullOrWhiteSpace( mode_.ToString() ) )
            {
                int numUniqueSpecies = (await _badgeRepo.CountByUserPerSpecies(user.Id)).Count;
                return new CommandResult
                {
                    Response = isSelf
                        ? $"You have collected {numUniqueSpecies} distinct Pokémon badge(s)"
                        : $"{user.Name} has collected {numUniqueSpecies} distinct Pokémon badge(s)"
                };
            }
            else
            {
                string mode = mode_.ToString();
                ImmutableSortedDictionary<PkmnSpecies, int> numBadgesPerSpecies =
                    await _badgeRepo.CountByUserPerSpecies(user.Id);

                if( mode.Equals( "missing" ))
                {
                    IEnumerable<PkmnSpecies> missingList = PokedexData.Load().KnownSpecies.Except( numBadgesPerSpecies.Keys );
                    // Seems like PokedexData is not sorted. So we have to sort the list.
                    IEnumerable<string> badgesFormatted = missingList.OrderBy( entry => entry.Id ).Select( entry => $"{entry}");
                    return new CommandResult
                    {
                        Response = isSelf
                            ? $"You are currently missing the following badge(s): {string.Join(", ", badgesFormatted)}"
                            : $"{user.Name} is currently missing the following badge(s): {string.Join(", ", badgesFormatted)}",
                        ResponseTarget = ResponseTarget.WhisperIfLong
                    };
                }
                else if( mode.Equals( "dupes" ) )
                {
                    var dupeList = numBadgesPerSpecies.Where(kvp => kvp.Value > 1);
                    if( !dupeList.Any() )
                    {
                        return new CommandResult
                        {
                            Response = isSelf
                                ? $"You do not own any duplicate Pokémon badges"
                                : $"{user.Name} does not own any duplicate Pokémon badges"
                        };
                    }
                    else
                    {
                        IEnumerable<string> badgesFormatted = dupeList.Select( kvp => $"{kvp.Key}");
                        return new CommandResult
                        {
                            Response = isSelf
                                ? $"You are owning duplicates of the following badge(s): {string.Join(", ", badgesFormatted)}"
                                : $"{user.Name} is owning duplicates of the following badge(s): {string.Join(", ", badgesFormatted)}",
                            ResponseTarget = ResponseTarget.WhisperIfLong
                        };
                    }
                }
                else
                {
                    return new CommandResult
                    {
                        Response =  $"Unsupported mode '{mode}'. Current modes supported: missing, dupes"
                    };
                }
            }
        }

        public async Task<CommandResult> GiftBadge(CommandContext context)
        {
            User gifter = context.Message.User;
            (User recipient, PkmnSpecies species, Optional<PositiveInt> amountOpt) =
                await context.ParseArgs<AnyOrder<User, PkmnSpecies, Optional<PositiveInt>>>();
            int amount = amountOpt.Map(i => i.Number).OrElse(1);

            if (recipient == gifter)
                return new CommandResult { Response = "You cannot gift to yourself" };

            List<Badge> badges = await _badgeRepo.FindByUserAndSpecies(gifter.Id, species);
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
            return new CommandResult
            {
                Response = amount > 1
                    ? $"has gifted {amount} {species} badges to {recipient.Name}!"
                    : $"has gifted a {species} badge to {recipient.Name}!",
                ResponseTarget = ResponseTarget.Chat
            };
        }
    }
}
