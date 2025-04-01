using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Core.Chat;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class CurrencyCommands(
    IBank<User> pokeyenBank,
    IBank<User> tokenBank,
    IMessageSender messageSender
) : ICommandCollection
{
    public IEnumerable<Command> Commands =>
    [
        new("balance", CheckBalance)
        {
            Aliases = ["pokeyen", "tokens", "currency", "money", "rank"],
            Description = "Show a user's pokeyen and tokens. Argument: <username> (optional)"
        },
        new("highscore", CheckHighScore)
        {
            Description = "Show a user's pokeyen high score for this season. Argument: <username> (optional)"
        },
        new("gifttokens", Donate)
        {
            Description = "Donate another user tokens. Arguments: <user to donate to> <amount of tokens>"
        }
    ];

    public async Task<CommandResult> CheckBalance(CommandContext context)
    {
        var optionalUser = await context.ParseArgs<Optional<User>>();
        bool isSelf = !optionalUser.IsPresent;
        User user = isSelf ? context.Message.User : optionalUser.Value;
        // Only differentiate between available and reserved money for self.
        // For others, just report their total as available and ignore reserved.
        long availablePokeyen = isSelf ? await pokeyenBank.GetAvailableMoney(user) : user.Pokeyen;
        long reservedPokeyen = isSelf ? await pokeyenBank.GetReservedMoney(user) : 0;
        long availableTokens = isSelf ? await tokenBank.GetAvailableMoney(user) : user.Tokens;
        long reservedTokens = isSelf ? await tokenBank.GetReservedMoney(user) : 0;

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

    public async Task<CommandResult> Donate(CommandContext context)
    {
        User gifter = context.Message.User;
        (User recipient, Tokens tokens) = await context.ParseArgs<AnyOrder<User, Tokens>>();
        if (recipient == gifter)
            return new CommandResult { Response = "You can't donate to yourself." };
        if (recipient.Banned)
            return new CommandResult { Response = "You cannot donate to a banned user." };
        if (tokens <= 0)
            return new CommandResult { Response = "You must donate at least T1." };

        long availableTokens = await tokenBank.GetAvailableMoney(gifter);
        if (availableTokens < tokens)
            return new CommandResult
            {
                Response = $"You are trying to donate T{tokens} but you only have T{availableTokens}."
            };

        await tokenBank.PerformTransactions([
            new Transaction<User>(gifter, -tokens, TransactionType.DonationGive),
            new Transaction<User>(recipient, tokens, TransactionType.DonationReceive)
        ]);

        await messageSender.SendWhisper(recipient, $"You have been donated T{tokens} from {gifter.Name}!");
        await messageSender.SendMessage($"{gifter.Name} has donated T{tokens} to @{recipient.Name}!");
        return new CommandResult
        {
            Response = $"You successfully donated T{tokens} to @{recipient.Name}!",
            ResponseTarget = ResponseTarget.NoneIfChat
        };
    }
}
