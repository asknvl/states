using MongoDB.Driver;
using states.Mongo.Documents.Folders;

namespace states.Mongo.Repositories
{
    public class FoldersRepository : IFoldersRepository
    {
        private readonly IMongoCollection<FolderDocument> collection;

        public FoldersRepository(MongoContext context)
        {
            collection = context.Folders;
        }

        public async Task<FolderDocument> Create(
            Guid tenantId,
            Guid spaceId,
            Guid funnelId,
            string name,
            int order,
            bool isHidden = false,
            CancellationToken ct = default)
        {
            var document = new FolderDocument
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                SpaceId = spaceId,
                FunnelId = funnelId,
                Name = name,
                Order = order,
                IsHidden = isHidden,
                CreatedAt = DateTime.UtcNow
            };

            await collection.InsertOneAsync(document, cancellationToken: ct);
            return document;
        }

        public async Task<IReadOnlyCollection<FolderDocument>> GetFolders(
            Guid tenantId,
            Guid? spaceId,
            Guid funnelId,
            CancellationToken ct = default)
        {
            var filters = new List<FilterDefinition<FolderDocument>>
            {
                Builders<FolderDocument>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<FolderDocument>.Filter.Eq(x => x.FunnelId, funnelId),
                Builders<FolderDocument>.Filter.Ne(x => x.IsHidden, true),
            };

            if (spaceId.HasValue)
                filters.Add(Builders<FolderDocument>.Filter.Eq(x => x.SpaceId, spaceId.Value));

            return await collection
                .Find(Builders<FolderDocument>.Filter.And(filters))
                .SortBy(x => x.Order)
                .ToListAsync(ct);
        }

        public async Task<FolderDocument> Update(
            Guid folderId,
            string? name,
            int? order,
            CancellationToken ct = default)
        {
            var updates = new List<UpdateDefinition<FolderDocument>>();

            if (name is not null)
                updates.Add(Builders<FolderDocument>.Update.Set(x => x.Name, name));
            if (order.HasValue)
                updates.Add(Builders<FolderDocument>.Update.Set(x => x.Order, order.Value));

            var update = Builders<FolderDocument>.Update.Combine(updates);
            var options = new FindOneAndUpdateOptions<FolderDocument> { ReturnDocument = ReturnDocument.After };

            var result = await collection.FindOneAndUpdateAsync(
                Builders<FolderDocument>.Filter.Eq(x => x.Id, folderId),
                update, options, ct);

            if (result is null)
                throw new KeyNotFoundException($"Folder with id '{folderId}' was not found.");

            return result;
        }

        public async Task Delete(Guid folderId, CancellationToken ct = default)
        {
            var result = await collection.DeleteOneAsync(
                Builders<FolderDocument>.Filter.Eq(x => x.Id, folderId),
                cancellationToken: ct);

            if (result.DeletedCount == 0)
                throw new KeyNotFoundException($"Folder with id '{folderId}' was not found.");
        }

        public async Task<IReadOnlyCollection<FolderDocument>> Reorder(
            Guid tenantId,
            Guid? spaceId,
            Guid funnelId,
            IReadOnlyList<Guid> orderedIds,
            CancellationToken ct = default)
        {
            var bulkOps = orderedIds
                .Select((id, index) => (WriteModel<FolderDocument>)new UpdateOneModel<FolderDocument>(
                    Builders<FolderDocument>.Filter.Eq(x => x.Id, id),
                    Builders<FolderDocument>.Update.Set(x => x.Order, (index + 1) * 1000)))
                .ToList();

            await collection.BulkWriteAsync(bulkOps, cancellationToken: ct);

            return await GetFolders(tenantId, spaceId, funnelId, ct);
        }
    }
}
