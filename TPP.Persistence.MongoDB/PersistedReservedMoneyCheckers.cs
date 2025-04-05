using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using TPP.Model;

namespace TPP.Persistence.MongoDB;

/// <summary>
/// In order to prevent token overspending in a setup where both the old and new core run simultaneously,
/// they both need to know about _all_ reserved tokens (see <see cref="IBank{T}"/> for info on reserved tokens).
/// The new core does not reserve any tokens yet, and the old core saves all reserved tokens to the database.
/// This class then knows all places in the database it needs to look.
/// </summary>
public class PersistedReservedMoneyCheckers
{
    private readonly IMongoDatabase _database;
    public PersistedReservedMoneyCheckers(IMongoDatabase database) => _database = database;

    public async Task<long> AllDatabaseReservedTokens(User user) =>
        await PinballReservedTokens(user) +
        await BadgeBuyOffersReservedTokens(user) +
        await ItemBuyOffersReservedTokens(user) +
        await SidegameReservedTokens(user);

    private async Task<long> PinballReservedTokens(User user) =>
        (await (
            from doc in _database.GetCollection<BsonDocument>("misc").AsQueryable()
            where doc["_id"] == "pinball_state" && doc["users"][user.Id] != BsonNull.Value
            select doc["users"][user.Id]
        ).ToListAsync()).Sum(i => i.ToInt32());

    private async Task<long> BadgeBuyOffersReservedTokens(User user) =>
        (await (
            from doc in _database.GetCollection<BsonDocument>("badgebuyoffers").AsQueryable()
            where doc["user"] == user.Id
            select new { Price = doc["price"], Amount = doc["amount"] }
        ).ToListAsync()).Sum(obj => obj.Price.ToInt32() * obj.Amount.ToInt32());

    private async Task<long> ItemBuyOffersReservedTokens(User user) =>
        (await (
            from doc in _database.GetCollection<BsonDocument>("itembuyoffers").AsQueryable()
            where doc["user_id"] == user.Id
            select new { Offer = doc["offer"], Quantity = doc["quantity"] }
        ).ToListAsync()).Sum(obj => obj.Offer.ToInt32() * obj.Quantity.ToInt32());

    private async Task<long> SidegameReservedTokens(User user) =>
        (await (
            from doc in _database.GetCollection<BsonDocument>("misc").AsQueryable()
            where doc["_id"] == "sidegame_reserved_tokens" && doc["users"][user.Id] != BsonNull.Value
            select doc["users"][user.Id]
        ).ToListAsync()).Sum(i => i.ToInt32());
}
