using MongoDB.Driver;
using states.Mongo.Documents;
using states.Mongo.Documents.Folders;
using states.Mongo.Documents.Outbox;
using states.Mongo.Documents.TenantTags;

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
            await CreateFoldersCollection(ct);
            await CreateFoldersIndexes(ct);
            await CreateTenantTagsCollection(ct);
            await CreateTenantTagsIndexes(ct);
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

        private async Task CreateOutboxCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("outbox"))
                return;

            await database.CreateCollectionAsync("outbox", cancellationToken: ct);
        }
        #endregion

        private async Task CreateFoldersCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("folders"))
                return;

            await database.CreateCollectionAsync("folders", cancellationToken: ct);
        }

        private async Task CreateTenantTagsCollection(CancellationToken ct)
        {
            var collectionNames = await database.ListCollectionNames().ToListAsync(ct);
            if (collectionNames.Contains("tenant_tags"))
                return;
            await database.CreateCollectionAsync("tenant_tags", cancellationToken: ct);
        }

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
                // один стейт на (тенант, воронка, лид); FunnelId=null — органика, тоже уникальна по (tenantId, null, leadId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.FunnelId)
                        .Ascending(x => x.LeadId),
                    new CreateIndexOptions { Unique = true }),

                // GetLeadStateByChatId(tenantId, botId, chatId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.BotId)
                        .Ascending(x => x.ChatId)),

                // GetLeadStateByChatId(tenantId, chatId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.ChatId)),

                // GetLeadStateByLeadId(tenantId, leadId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.LeadId)),
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
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

        private async Task CreateFoldersIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FolderDocument>("folders");

            var indexes = new List<CreateIndexModel<FolderDocument>>
            {
                // покрывает фильтр tenantId+funnelId и sort по order без in-memory сортировки
                new CreateIndexModel<FolderDocument>(
                    Builders<FolderDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.FunnelId)
                        .Ascending(x => x.Order))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateTenantTagsIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<TenantTag>("tenant_tags");

            var indexes = new List<CreateIndexModel<TenantTag>>
            {
                new CreateIndexModel<TenantTag>(
                    Builders<TenantTag>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.TagName))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        #endregion
    }
}
