using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;
using TPP.Match;
using TPP.Persistence.Models;

namespace TPP.Core.Commands.Definitions
{
    public class BettingCommands : ICommandCollection
    {
        private readonly Func<IBettingShop<User>?> _bettingShopProvider;

        /// <summary>
        /// Command collection for all betting related commands.
        /// </summary>
        /// <param name="bettingShopProvider">Provides the current betting shop,
        /// which may be null if betting isn't betting is not available at that moment.</param>
        public BettingCommands(Func<IBettingShop<User>?> bettingShopProvider)
        {
            _bettingShopProvider = bettingShopProvider;
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
                return new CommandResult { Response = "You may only bet through chat" };
            IBettingShop<User>? bettingShop = _bettingShopProvider();
            if (bettingShop == null)
                return new CommandResult { Response = "betting not available right now" };
            (var amountOptions, Side side) = await context.ParseArgs<OneOf<PositiveInt, Pokeyen, Percentage>, Side>();
            int amount;
            if (amountOptions.Item1.IsPresent)
                amount = amountOptions.Item1.Value;
            else if (amountOptions.Item2.IsPresent)
                amount = amountOptions.Item2.Value;
            else
                amount = (int)Math.Ceiling(amountOptions.Item3.Value.AsRatio * context.Message.User.Pokeyen);

            PlaceBetFailure? failure = await bettingShop.PlaceBet(context.Message.User, side, amount);
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
}
