using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Serializers;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Repos
{
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
                cm.MapProperty(b => b.Votes).SetElementName("votes");
            });

            BsonClassMap.RegisterClassMap<Poll>(cm =>
            {
                cm.MapIdProperty(b => b.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(b => b.PollTitle).SetElementName("title");
                cm.MapProperty(b => b.PollCode).SetElementName("code");
                cm.MapProperty(b => b.Voters).SetElementName("voters");
                cm.MapProperty(b => b.PollOptions).SetElementName("options");
                cm.MapProperty(b => b.CreatedAt).SetElementName("created_at");
                cm.MapProperty(b => b.MultiChoice).SetElementName("multi");
                cm.MapProperty(b => b.Alive).SetElementName("alive");
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
            string pollTitle, string pollCode, bool multiChoice, IImmutableList<string> pollOptions)
        {
            List<PollOption> pollOptionsObjects = pollOptions
                .Select((option, index) => new PollOption(
                    id: index + 1,
                    option: option,
                    votes: 0,
                    voterIds: new List<string>()
                )).ToList();

            Poll poll = new(
                id: string.Empty,
                pollTitle: pollTitle,
                pollCode: pollCode,
                voters: new List<string>(),
                pollOptions: pollOptionsObjects,
                _clock.GetCurrentInstant(),
                multiChoice: multiChoice,
                alive: true
            );

            await Collection.InsertOneAsync(poll);
            Debug.Assert(poll.Id.Length > 0, "The MongoDB driver injected a generated ID");

            return poll;
        }

        public async Task<VoteFailure?> Vote(string pollCode, string userId, IImmutableList<int> options)
        {
            Poll? poll = await FindPoll(pollCode);
            if (poll == null)
                return new VoteFailure.PollNotFound(pollCode);
            if (!poll.Alive)
                return new VoteFailure.PollNotAlive();
            if (poll.Voters.Contains(userId))
                return new VoteFailure.AlreadyVoted();
            if (options.Count > 1 && !poll.MultiChoice)
                return new VoteFailure.NotMultipleChoice();

            await Collection.UpdateOneAsync(
                p => p.PollCode == pollCode,
                Builders<Poll>.Update.AddToSet(p => p.Voters, userId));

            ImmutableList<int> invalidOptions = options.Except(poll.PollOptions.Select(p => p.Id)).ToImmutableList();
            if (invalidOptions.Any())
                return new VoteFailure.InvalidOptions(invalidOptions);

            // the MongoDB C# driver's representation for '$', see also https://docs.mongodb.com/manual/reference/operator/update/positional/
            const int positionalOperator = -1;

            foreach (int option in options)
            {
                await Collection.UpdateOneAsync(
                    p => p.PollCode == pollCode && p.PollOptions.Any(o => o.Id == option),
                    Builders<Poll>.Update
                        .AddToSet(p => p.PollOptions[positionalOperator].VoterIds, userId)
                        .Inc(p => p.PollOptions[positionalOperator].Votes, 1));
            }

            return null;
        }

        public async Task<Poll?> FindPoll(string pollCode) =>
            await Collection.Find(p => p.PollCode == pollCode).FirstOrDefaultAsync();
    }
}
