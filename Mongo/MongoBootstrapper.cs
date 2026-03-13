using MongoDB.Driver;
using states.Mongo.Documents;

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
        #endregion

        #region indexes
        private async Task CreateFunnelsIndexes(CancellationToken ct)
        {
            var messages = database.GetCollection<Funnel>("preset_messages");

            var indexes = new List<CreateIndexModel<Funnel>>
            {
                new CreateIndexModel<Funnel>(
                    Builders<Funnel>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.BotId)
                        .Descending(x => x.Id))
            };

            await messages.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }
        #endregion
    }
}
