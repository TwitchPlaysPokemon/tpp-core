using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Commands.Definitions
{
    public class UserCommands : ICommandCollection
    {
        private const string UnlockGlowCommandName = "unlockglow";

        public IEnumerable<Command> Commands => new[]
        {
            new Command("balance", CheckBalance)
            {
                Aliases = new[] { "pokeyen", "tokens", "currency", "money", "rank" },
                Description = "Show a user's pokeyen and tokens. Argument: <username> (optional)"
            },
            new Command("highscore", CheckHighScore)
            {
                Description = "Show a user's pokeyen high score for this season. Argument: <username> (optional)"
            },
            new Command("setglow", SetGlow)
            {
                Aliases = new[] { "setsecondarycolor", "setsecondarycolour" },
                Description = "Change the color of the glow around your name as it appears on stream. " +
                              "Argument: #<hexcolor>"
            },
            new Command("removeglow", RemoveGlow)
            {
                Aliases = new[] { "removesecondarycolor", "removesecondarycolour" },
                Description = "Remove the glow around your name as it appears on stream."
            },
            new Command(UnlockGlowCommandName, UnlockGlow)
            {
                Aliases = new[] { "unlocksecondarycolor", "unlocksecondarycolour" },
                Description = "Unlock the ability to change the color of your glow around your name " +
                              "as it appears on stream. Costs 1 token."
            },
            new Command("displayname", SetDisplayName)
            {
                Description = "Set the capitalization of your display name as it appears on stream. " +
                              "Only needed by users with special characters in their display name. " +
                              "Argument: <new name>"
            },
            new Command("emblems", CheckEmblems)
            {
                Aliases = new[] { "participation" },
                Description = "Show a user's run participation record. Argument: <username> (optional)"
            },
            new Command("selectemblem", SelectEmblem)
            {
                Aliases = new[] { "chooseemblem", "selectparticipationbadge", "chooseparticipationbadge" },
                Description = "Select which run's emblem color to show on stream next to your name. " +
                              "Argument: <run number>"
            },
            new Command("donate", Donate)
            {
                Aliases = new[] { "gifttokens" },
                Description = "Donate another user tokens. Arguments: <user to donate to> <amount of tokens>"
            },
            new Command("list", List)
            {
                Description = "List all of the people who have a given role." +
                              "Arguemnts: <Role>"
            },
            new Command("showroles", ShowRoles)
            {
                Aliases = new[] {"roles"},
                Description = "Operators only: Show which roles a user has." +
                              "Arguments: <user>"
            },
        };

        private readonly IUserRepo _userRepo;
        private readonly IBank<User> _pokeyenBank;
        private readonly IBank<User> _tokenBank;
        private readonly IMessageSender _messageSender;

        public UserCommands(
            IUserRepo userRepo,
            IBank<User> pokeyenBank,
            IBank<User> tokenBank,
            IMessageSender messageSender)
        {
            _userRepo = userRepo;
            _pokeyenBank = pokeyenBank;
            _tokenBank = tokenBank;
            _messageSender = messageSender;
        }

        public async Task<CommandResult> CheckBalance(CommandContext context)
        {
            var optionalUser = await context.ParseArgs<Optional<User>>();
            bool isSelf = !optionalUser.IsPresent;
            User user = isSelf ? context.Message.User : optionalUser.Value;
            // Only differentiate between available and reserved money for self.
            // For others, just report their total as available and ignore reserved.
            long availablePokeyen = isSelf ? await _pokeyenBank.GetAvailableMoney(user) : user.Pokeyen;
            long reservedPokeyen = isSelf ? await _pokeyenBank.GetReservedMoney(user) : 0;
            long availableTokens = isSelf ? await _tokenBank.GetAvailableMoney(user) : user.Tokens;
            long reservedTokens = isSelf ? await _tokenBank.GetReservedMoney(user) : 0;

            string reservedPokeyenMessage = reservedPokeyen > 0 ? $" (P{reservedPokeyen} reserved)" : "";
            string reservedTokensMessage = reservedTokens > 0 ? $" (T{reservedTokens} reserved)" : "";
            string rankMessage = user.PokeyenBetRank == null
                ? ""
                : $" {(isSelf ? "You" : "They")} are currently rank {user.PokeyenBetRank} in the leaderboard.";
            string response =
                $"{(isSelf ? "You have" : $"{user.Name} has")}" +
                $" P{availablePokeyen} pokeyen{reservedPokeyenMessage}" +
                $" and T{availableTokens} tokens{reservedTokensMessage}." +
                rankMessage;
            return new CommandResult { Response = response };
        }

        public async Task<CommandResult> CheckHighScore(CommandContext context)
        {
            var optionalUser = await context.ParseArgs<Optional<User>>();
            bool isSelf = !optionalUser.IsPresent;
            User user = isSelf ? context.Message.User : optionalUser.Value;
            return new CommandResult
            {
                Response = user.PokeyenHighScore > 0
                    ? isSelf
                        ? $"Your high score is P{user.PokeyenHighScore}"
                        : $"{user.Name}'s high score is P{user.PokeyenHighScore}"
                    : isSelf
                        ? "You don't have a high score"
                        : $"{user.Name} doesn't have a high score."
            };
        }

        public async Task<CommandResult> SetGlow(CommandContext context)
        {
            User user = context.Message.User;
            if (!user.GlowColorUnlocked)
            {
                return new CommandResult
                {
                    Response = $"glow color is still locked, use '{UnlockGlowCommandName}' to unlock (costs T1)"
                };
            }
            string color = (await context.ParseArgs<HexColor>()).StringWithoutHash;
            await _userRepo.SetGlowColor(user, color);
            return new CommandResult { Response = $"glow color set to #{color}" };
        }

        public async Task<CommandResult> RemoveGlow(CommandContext context)
        {
            await _userRepo.SetGlowColor(context.Message.User, null);
            return new CommandResult { Response = "your glow color was removed" };
        }

        public async Task<CommandResult> UnlockGlow(CommandContext context)
        {
            User user = context.Message.User;
            if (user.GlowColorUnlocked)
            {
                return new CommandResult { Response = "glow color is already unlocked" };
            }
            if (await _tokenBank.GetAvailableMoney(user) < 1)
            {
                return new CommandResult { Response = "you don't have T1 to unlock the glow color" };
            }
            await _tokenBank.PerformTransaction(new Transaction<User>(user, -1, TransactionType.SecondaryColorUnlock));
            await _userRepo.SetGlowColorUnlocked(user, true);
            return new CommandResult { Response = "your glow color was unlocked" };
        }

        public async Task<CommandResult> SetDisplayName(CommandContext context)
        {
            User user = context.Message.User;
            if (user.TwitchDisplayName.ToLowerInvariant() == user.SimpleName)
            {
                return new CommandResult
                {
                    Response = "you don't have any special characters in your name " +
                               "and can therefore still change it in your twitch settings"
                };
            }
            string newName = await context.ParseArgs<string>();
            if (newName.ToLower() != user.SimpleName)
            {
                return new CommandResult
                {
                    Response = "your new display name may only differ from your login name in capitalization"
                };
            }
            await _userRepo.SetDisplayName(user, newName);
            return new CommandResult { Response = $"your display name has been updated to '{newName}'" };
        }

        public async Task<CommandResult> CheckEmblems(CommandContext context)
        {
            var optionalUser = await context.ParseArgs<Optional<User>>();
            bool isSelf = !optionalUser.IsPresent;
            User user = isSelf ? context.Message.User : optionalUser.Value;
            if (user.ParticipationEmblems.Any())
            {
                string formattedEmblems = Emblems.FormatEmblems(user.ParticipationEmblems);
                return new CommandResult
                {
                    Response = isSelf
                        ? $"you have participated in the following runs: {formattedEmblems}"
                        : $"{user.Name} has participated in the following runs: {formattedEmblems}",
                    ResponseTarget = ResponseTarget.WhisperIfLong
                };
            }
            else
            {
                return new CommandResult
                {
                    Response = isSelf
                        ? "you have not participated in any runs"
                        : $"{user.Name} has not participated in any runs"
                };
            }
        }

        public async Task<CommandResult> SelectEmblem(CommandContext context)
        {
            User user = context.Message.User;
            int emblem = await context.ParseArgs<NonNegativeInt>();
            if (!user.ParticipationEmblems.Contains(emblem))
            {
                return new CommandResult { Response = "you don't own that participation badge" };
            }
            await _userRepo.SetSelectedEmblem(user, emblem);
            return new CommandResult
            {
                Response = $"color of participation badge {Emblems.FormatEmblem(emblem)} successfully equipped"
            };
        }

        public async Task<CommandResult> Donate(CommandContext context)
        {
            User gifter = context.Message.User;
            (User recipient, Tokens tokens) = await context.ParseArgs<AnyOrder<User, Tokens>>();
            if (recipient == gifter)
                return new CommandResult { Response = "You can't donate to yourself." };
            if (tokens <= 0)
                return new CommandResult { Response = "You must donate at least T1." };

            long availableTokens = await _tokenBank.GetAvailableMoney(gifter);
            if (availableTokens < tokens)
                return new CommandResult
                {
                    Response = $"You are trying to donate T{tokens} but you only have T{availableTokens}."
                };

            await _tokenBank.PerformTransactions(new[]
            {
                new Transaction<User>(gifter, -tokens, TransactionType.DonationGive),
                new Transaction<User>(recipient, tokens, TransactionType.DonationReceive)
            });

            await _messageSender.SendWhisper(recipient, $"You have been donated T{tokens} from {gifter.Name}!");
            return new CommandResult
            {
                Response = $"has donated T{tokens} to @{recipient.Name}!",
                ResponseTarget = ResponseTarget.Chat
            };
        }

        public async Task<CommandResult> List(CommandContext context)
        {
            Role role = await context.ParseArgs<Role>();
            List<User> users = await _userRepo.FindAllByRole(role);

            return new CommandResult
            {
                Response = users.Count > 0
                    ? $"The users with the '{role.ToString()}' role are: {string.Join(", ", users.Select(u => u.Name))}"
                    : $"There are no users with the '{role.ToString()}' role."
            };
        }
        public async Task<CommandResult> ShowRoles(CommandContext context)
        {
            User user = await context.ParseArgs<User>();

            return new CommandResult
            {
                Response = user.Roles.Count > 0
                    ? $"{user.Name} has the roles: {string.Join(", ", user.Roles)}"
                    : $"{user.Name} has no roles"
            };
        }
    }
}
