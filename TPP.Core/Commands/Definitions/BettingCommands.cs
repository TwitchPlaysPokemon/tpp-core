using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Match;
using TPP.Model;

namespace TPP.Core.Commands.Definitions;

public class BettingCommands : ICommandCollection
{
    private readonly Func<IBettingPeriod<User>?> _bettingPeriodProvider;

    /// <summary>
    /// Command collection for all betting related commands.
    /// </summary>
    /// <param name="bettingPeriodProvider">Provides the current betting period,
    /// which may be null if there currently is no betting period running.</param>
    public BettingCommands(Func<IBettingPeriod<User>?> bettingPeriodProvider)
    {
        _bettingPeriodProvider = bettingPeriodProvider;
    }

    public IEnumerable<Command> Commands => new[]
    {
        new Command("bet", Bet)
        {
            Description = "Chat only: bet on PBR using Pokéyen. Arguments: <team> <amount or percent>"
        }
    };

    public async Task<CommandResult> Bet(CommandContext context)
    {
        if (context.Message.MessageSource != MessageSource.Chat)
            return new CommandResult { Response = "you may only bet through chat" };
        IBettingPeriod<User>? bettingPeriod = _bettingPeriodProvider();
        if (bettingPeriod == null)
            return new CommandResult { Response = "betting not available right now" };
        if (!bettingPeriod.IsBettingOpen)
            return new CommandResult { Response = "betting is already closed" };
        (var amountOptions, Side side) = await context.ParseArgs<OneOf<PositiveInt, Pokeyen, Percentage>, Side>();
        int amount;
        if (amountOptions.Item1.IsPresent)
            amount = amountOptions.Item1.Value;
        else if (amountOptions.Item2.IsPresent)
            amount = amountOptions.Item2.Value;
        else
            amount = (int)Math.Ceiling(amountOptions.Item3.Value.AsDecimal * context.Message.User.Pokeyen);

        PlaceBetFailure? failure = await bettingPeriod.BettingShop.PlaceBet(context.Message.User, side, amount);
        if (failure != null)
        {
            return new CommandResult
            {
                Response = failure switch
                {
                    PlaceBetFailure.BetTooHigh betTooHigh =>
                        $"must bet at most {betTooHigh.MaxBet}",
                    PlaceBetFailure.BetTooLow betTooLow =>
                        $"must bet at least {betTooLow.MinBet}",
                    PlaceBetFailure.CannotChangeSide cannotChangeSide =>
                        $"already bet on {cannotChangeSide.SideBetOn}",
                    PlaceBetFailure.CannotLowerBet cannotLowerBet =>
                        $"cannot lower existing bet of {cannotLowerBet.ExistingBet}",
                    PlaceBetFailure.InsufficientFunds insufficientFunds =>
                        $"insufficient funds, you only have {insufficientFunds.AvailableMoney} pokeyen available",
                    _ => $"{failure} (missing text, tell the devs to fix this)"
                }
            };
        }
        return new CommandResult { Response = $"placed a P{amount} bet on {side}." };
    }
}
