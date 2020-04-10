using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Persistence.Models;

namespace Persistence.MongoDB.Serializers
{
    public class TransactionTypeSerializer : EnumToStringUsingTranslationMappingSerializer<TransactionType>
    {
        public static readonly TransactionTypeSerializer Instance = new TransactionTypeSerializer();

        private TransactionTypeSerializer() : base(new Dictionary<TransactionType, string>
        {
            [TransactionType.Unknown] = "unknown",
            [TransactionType.SidegameStorm] = "sidegame_storm",
            [TransactionType.SidegameStormPayment] = "sidegame_storm_payment",
            [TransactionType.SidegameBribe] = "sidegame_bribe",
            [TransactionType.SidegameBribePayment] = "sidegame_bribe_payment",
            [TransactionType.DonationTokens] = "donation_tokens",
            [TransactionType.DonationRandomlyDistributedTokens] = "donation_randomly_distributed_tokens",
            [TransactionType.BadgeSell] = "badge_sell",
            [TransactionType.BadgeBuy] = "badge_buy",
            [TransactionType.Songbid] = "songbid",
            [TransactionType.Tokenmatchbid] = "tokenmatchbid",
            [TransactionType.Pinball] = "pinball",
            [TransactionType.Transmutation] = "transmutation",
            [TransactionType.Subscription] = "subscription",
            [TransactionType.Liquidation] = "liquidation",
            [TransactionType.LiquidationWinner] = "liquidation_winner",
            [TransactionType.Crate] = "crate",
            [TransactionType.CheerfulSlots] = "cheerful slots",
            [TransactionType.LevelUp] = "level_up",
            [TransactionType.SecondaryColorUnlock] = "secondary_color_unlock",
            [TransactionType.Test] = "test",
            [TransactionType.ManualAdjustment] = "manual_adjustment",
        })
        {
        }

        public override TransactionType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context.Reader.CurrentBsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return TransactionType.Unknown;
            }
            else
            {
                return base.Deserialize(context, args);
            }
        }
    }
}
