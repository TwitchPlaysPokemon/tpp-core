using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Core.Chat;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class CurrencyCommands : ICommandCollection
{
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
        new Command("donate", Donate)
        {
            Aliases = new[] { "gifttokens" },
            Description = "Donate another user tokens. Arguments: <user to donate to> <amount of tokens>"
        },
    };

    private readonly IBank<User> _pokeyenBank;
    private readonly IBank<User> _tokenBank;
    private readonly IMessageSender _messageSender;

    public CurrencyCommands(
        IBank<User> pokeyenBank,
        IBank<User> tokenBank,
        IMessageSender messageSender)
    {
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
        await _messageSender.SendMessage($"{gifter.Name} has donated T{tokens} to @{recipient.Name}!");
        return new CommandResult
        {
            Response = $"You successfully donated T{tokens} to @{recipient.Name}!",
            ResponseTarget = ResponseTarget.NoneIfChat
        };
    }
}
