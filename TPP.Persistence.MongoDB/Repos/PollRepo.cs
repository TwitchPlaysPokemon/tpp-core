using static System.Linq.Enumerable;

namespace TPP.Persistence.MongoDB.Repos;

public class PollRepo : IPollRepo
{
    private const string CollectionName = "polls";

    public readonly IMongoCollection<Poll> Collection;

    private readonly IClock _clock;

    static PollRepo()
    {
        BsonClassMap.RegisterClassMap<PollOption>(cm =>
        {
            cm.MapIdProperty(b => b.Id).SetElementName("id");
            cm.MapProperty(b => b.Option).SetElementName("option");
            cm.MapProperty(b => b.VoterIds).SetElementName("voters");
        });

        BsonClassMap.RegisterClassMap<Poll>(cm =>
        {
            cm.MapIdProperty(b => b.PollCode);
            cm.MapProperty(b => b.PollTitle).SetElementName("title");
            cm.MapProperty(b => b.Voters).SetElementName("voters");
            cm.MapProperty(b => b.PollOptions).SetElementName("options");
            cm.MapProperty(b => b.CreatedAt).SetElementName("created_at");
            cm.MapProperty(b => b.MultiChoice).SetElementName("multi");
            cm.MapProperty(b => b.Alive).SetElementName("alive");
            cm.MapProperty(b => b.AllowChangeVote).SetElementName("allow_change_vote");
        });
    }

    public PollRepo(IMongoDatabase database, IClock clock)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<Poll>(CollectionName);
        _clock = clock;
        InitIndexes();
    }

    private void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Poll>(Builders<Poll>.IndexKeys.Ascending(u => u.PollCode)),
            new CreateIndexModel<Poll>(Builders<Poll>.IndexKeys.Ascending(u => u.CreatedAt)),
        });
    }

    public async Task<Poll> CreatePoll(
        string pollCode, string pollTitle, bool multiChoice, bool allowChangeVote,
        IImmutableList<string> pollOptions)
    {
        List<PollOption> pollOptionsObjects = pollOptions
            .Select((option, index) => new PollOption(
                id: index + 1,
                option: option,
                voterIds: new List<string>()
            )).ToList();

        Poll poll = new(
            pollCode: pollCode,
            pollTitle: pollTitle,
            voters: new List<string>(),
            pollOptions: pollOptionsObjects,
            _clock.GetCurrentInstant(),
            multiChoice: multiChoice,
            alive: true,
            allowChangeVote: allowChangeVote
        );

        await Collection.InsertOneAsync(poll);

        return poll;
    }

    public async Task<VoteFailure?> Vote(string pollCode, string userId, IImmutableList<int> options)
    {
        Poll? poll = await FindPoll(pollCode);
        if (poll == null)
            return new VoteFailure.PollNotFound(pollCode);
        if (!poll.Alive)
            return new VoteFailure.PollNotAlive();
        if (!poll.AllowChangeVote && poll.Voters.Contains(userId))
            return new VoteFailure.AlreadyVoted();
        if (options.Count == 0)
            return new VoteFailure.CannotVoteForNone();
        if (options.Count > 1 && !poll.MultiChoice)
            return new VoteFailure.NotMultipleChoice();

        ImmutableList<int> invalidOptions = options.Except(poll.PollOptions.Select(p => p.Id)).ToImmutableList();
        if (invalidOptions.Any())
            return new VoteFailure.InvalidOptions(invalidOptions);

        await Collection.UpdateOneAsync(
            p => p.PollCode == pollCode,
            Builders<Poll>.Update.AddToSet(p => p.Voters, userId));

        // remove any existing votes in case the user has voted before and is changing their vote right now.
        // no typed support for the $[] (for each in array) operator yet, see https://jira.mongodb.org/browse/CSHARP-2232
        foreach (int i in Range(0, poll.PollOptions.Count))
            await Collection.UpdateOneAsync(
                p => p.PollCode == pollCode,
                Builders<Poll>.Update.Pull(p => p.PollOptions[i].VoterIds, userId));

        // the MongoDB C# driver's representation for '$', see also https://docs.mongodb.com/manual/reference/operator/update/positional/
        const int positionalOperator = -1;

        foreach (int option in options)
        {
            await Collection.UpdateOneAsync(
                p => p.PollCode == pollCode && p.PollOptions.Any(o => o.Id == option),
                Builders<Poll>.Update.AddToSet(p => p.PollOptions[positionalOperator].VoterIds, userId));
        }

        return null;
    }

    public async Task<Poll?> FindPoll(string pollCode) =>
        await Collection.Find(p => p.PollCode == pollCode).FirstOrDefaultAsync();

    public async Task<IImmutableList<Poll>> FindPolls(bool onlyActive) =>
        (await Collection
            .Find(onlyActive ? Builders<Poll>.Filter.Eq(p => p.Alive, true) : FilterDefinition<Poll>.Empty)
            .ToListAsync()).ToImmutableList();

    public async Task<bool?> SetAlive(string id, bool alive)
    {
        UpdateResult updateOneAsync = await Collection.UpdateOneAsync(
            p => p.PollCode == id,
            Builders<Poll>.Update.Set(p => p.Alive, alive));
        if (updateOneAsync.MatchedCount == 0) return null;
        // by virtue of dealing with a boolean we can imply the previous value from whether it was modified
        return updateOneAsync.ModifiedCount > 0 ? !alive : alive;
    }
}
