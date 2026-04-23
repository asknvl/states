using MongoDB.Driver;
using states.Mongo.Documents;
using states.Mongo.Documents.Outbox;

namespace states.Mongo
{
    public class MongoBootstrapper
    {
        private readonly IMongoDatabase database;

        public MongoBootstrapper(IMongoDatabase database)
        {
            this.database = database;
        }

        public async Task Initialize(CancellationToken ct = default)
        {
            await CreateFunnelsCollection(ct);
            await CreateFunnelsIndexes(ct);
            await CreateLeadStatesCollection(ct);
            await CreateLeadStatesIndexes(ct);
            await CreateOutboxCollection(ct);
            await CreateOutboxIndexes(ct);
        }

        #region collections
        private async Task CreateFunnelsCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("funnels"))
                return;

            await database.CreateCollectionAsync("funnels", cancellationToken: ct);
        }

        private async Task CreateLeadStatesCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("lead_states"))
                return;

            await database.CreateCollectionAsync("lead_states", cancellationToken: ct);
        }
        #endregion

        #region indexes
        private async Task CreateFunnelsIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FunnelDocument>("funnels");

            var indexes = new List<CreateIndexModel<FunnelDocument>>
            {
                new CreateIndexModel<FunnelDocument>(
                    Builders<FunnelDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Descending(x => x.Id))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateLeadStatesIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FunnelLeadState>("lead_states");

            var indexes = new List<CreateIndexModel<FunnelLeadState>>
            {
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.FunnelId)
                        .Ascending(x => x.LeadId),
                    new CreateIndexOptions { Unique = true })
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateOutboxCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("outbox"))
                return;

            await database.CreateCollectionAsync("outbox", cancellationToken: ct);
        }

        private async Task CreateOutboxIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<OutboxDocument>("outbox");

            var indexes = new List<CreateIndexModel<OutboxDocument>>
            {
                // покрывает фильтр по claimedAt и сортировку по createdAt в одном проходе
                new CreateIndexModel<OutboxDocument>(
                    Builders<OutboxDocument>.IndexKeys
                        .Ascending(x => x.ClaimedAt)
                        .Ascending(x => x.CreatedAt))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }
        #endregion
    }
}
