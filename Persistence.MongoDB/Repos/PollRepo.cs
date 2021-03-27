using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using Persistence.Models;
using Persistence.MongoDB.Serializers;
using Persistence.Repos;

namespace Persistence.MongoDB.Repos
{
    public class PollRepo : IPollRepo
    {
        private const string CollectionName = "polls";

        public readonly IMongoCollection<Poll> Collection;

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

        public PollRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<Poll>(CollectionName);
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

        public async Task<Poll> CreatePoll(string pollTitle, string pollCode, bool multiChoice, string[] pollOptions)
        {
            PollOption[] pollOptionsArray = new PollOption[] { };

            foreach (string option in pollOptions)
            {
                PollOption pollOpt = new PollOption(
                    id: pollOptionsArray.Length + 1,
                    option: option,
                    votes: 0,
                    voterIds: new List<string>()
                );

                List<PollOption> pollOptList = pollOptionsArray.ToList();
                pollOptList.Add(pollOpt);
                pollOptionsArray = pollOptList.ToArray();
            }

            Poll poll = new Poll(
                id: string.Empty,
                pollTitle: pollTitle,
                pollCode: pollCode,
                voters: new List<string>(),
                pollOptions: pollOptionsArray,
                Instant.FromUnixTimeSeconds(0),
                multiChoice: multiChoice,
                alive: true
            ); ;

            await Collection.InsertOneAsync(poll);
            Debug.Assert(poll.Id.Length > 0, "The MongoDB driver injected a generated ID");

            Trace.WriteLine(poll.Id);
            return poll;
        }

        public async Task<Poll> Vote(string pollCode, string userId, string[] options, bool useIntArgs)
        {
            FilterDefinitionBuilder<Poll> filter = Builders<Poll>.Filter;
            UpdateDefinitionBuilder<Poll> update = Builders<Poll>.Update;

            FilterDefinition<Poll> pollFilter = filter.Eq(p => p.PollCode, pollCode);
            UpdateDefinition<Poll> pollVoter = update.AddToSet("voters", userId);
            await Collection.UpdateOneAsync(pollFilter, pollVoter);

            foreach (var option in options)
            {
                if (useIntArgs)
                {
                    FilterDefinition<Poll> pollOptionsIdFilter = filter.And(
                        filter.Eq(p => p.PollCode, pollCode),
                        filter.ElemMatch(p => p.PollOptions, o => o.Id == int.Parse(option)));

                    UpdateDefinition<Poll> pollOptionVoter = update.AddToSet("options.$.voters", userId);
                    UpdateDefinition<Poll> pollOptiopnVoteInc = update.Inc("options.$.votes", 1);

                    await Collection.UpdateOneAsync(pollOptionsIdFilter, pollOptionVoter);
                    await Collection.UpdateOneAsync(pollOptionsIdFilter, pollOptiopnVoteInc);
                }
                else
                {
                    FilterDefinition<Poll> pollOptionsIdFilter = filter.And(
                        filter.Eq(p => p.PollCode, pollCode),
                        filter.ElemMatch(p => p.PollOptions, o => o.Option == option));

                    UpdateDefinition<Poll> pollOptionVoter = update.AddToSet("options.$.voters", userId);
                    UpdateDefinition<Poll> pollOptiopnVoteInc = update.Inc("options.$.votes", 1);

                    await Collection.UpdateOneAsync(pollOptionsIdFilter, pollOptionVoter);
                    await Collection.UpdateOneAsync(pollOptionsIdFilter, pollOptiopnVoteInc);
                }
            }

            return await Collection.Find(p => p.PollCode == pollCode).FirstAsync();
        }

        public async Task<bool> IsMulti(string pollCode) =>
            await
                Collection
                .Find(p => (p.PollCode == pollCode) && p.MultiChoice)
                .AnyAsync();

        public async Task<bool> HasVoted(string pollCode, string userId) =>
            await
                Collection
                .Find(p => (p.PollCode == pollCode && p.Voters.Contains(userId)))
                .AnyAsync();

        public async Task<bool> IsPollValid(string pollCode) =>
            await
                Collection
                .Find(p => (p.PollCode == pollCode) && p.Alive)
                .AnyAsync();

        public async Task<bool> IsVoteValid(string pollCode, string[] votes, bool useIntArgs)
        {
            var isValid = true;
            FilterDefinitionBuilder<Poll> filter = Builders<Poll>.Filter;
            FilterDefinition<Poll> pollFilter = filter.Eq(p => p.PollCode, pollCode);

            foreach (string vote in votes)
            {
                if (isValid)
                {
                    if (useIntArgs)
                    {
                        FilterDefinition<Poll> pollOptionsIdFilter = filter.And(
                            filter.Eq(p => p.PollCode, pollCode),
                            filter.ElemMatch(p => p.PollOptions, o => o.Id == int.Parse(vote)));

                        isValid = await Collection.Find(pollOptionsIdFilter).AnyAsync();
                    }
                    else
                    {
                        FilterDefinition<Poll> pollOptionsIdFilter = filter.And(
                            filter.Eq(p => p.PollCode, pollCode),
                            filter.ElemMatch(p => p.PollOptions, o => o.Option == vote));

                        isValid = await Collection.Find(pollOptionsIdFilter).AnyAsync();
                    }
                }
            }

            return isValid;
        }
    }
}
