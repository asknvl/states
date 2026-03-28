using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using states.Mongo.Documents;

namespace states.Mongo.Repositories
{
    public class FunnelsRepository : IFunnelsRepository
    {
        private readonly IMongoCollection<FunnelDocument> collection;

        public FunnelsRepository(MongoContext context)
        {
            collection = context.Funnels;
        }

        #region public
        public async Task Create(FunnelDocument document, CancellationToken ct)
        {
            try
            {

                await collection.InsertOneAsync(document);

            } catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw;
            }
        }

        public async Task UpdateMetadata(Guid funnelId, string name, string? description, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId);
            var update = Builders<FunnelDocument>.Update
                .Set(x => x.Name, name)
                .Set(x => x.Description, description);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
        }

        public async Task AddFlow(Guid funnelId, FlowDocument flow, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId);
            var update = Builders<FunnelDocument>.Update.Push(x => x.Flows, flow);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
        }

        public async Task UpdateFlow(Guid funnelId, FlowDocument flow, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.And(
                Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId),
                Builders<FunnelDocument>.Filter.ElemMatch(x => x.Flows, f => f.Id == flow.Id)
            );
            var update = Builders<FunnelDocument>.Update.Set(x => x.Flows.FirstMatchingElement(), flow);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Flow with id '{flow.Id}' was not found in funnel '{funnelId}'.");
        }

        public async Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId);
            var update = Builders<FunnelDocument>.Update.PullFilter(x => x.Flows, f => f.Id == flowId);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
        }

        public async Task<IReadOnlyCollection<FunnelDocument>> GetByTenant(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct)
        {
            var filters = new List<FilterDefinition<FunnelDocument>>
            {
                Builders<FunnelDocument>.Filter.Eq(x => x.TenantId, tenantId)
            };

            if (spaceId.HasValue)
                filters.Add(Builders<FunnelDocument>.Filter.Eq(x => x.SpaceId, spaceId.Value));

            var filter = Builders<FunnelDocument>.Filter.And(filters);

            return await collection.Find(filter).ToListAsync(ct);
        }

        public async Task<FunnelDocument> Get(Guid funnelId)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId);

            var document = await collection
                .Find(filter)
                .FirstOrDefaultAsync();

            if (document is null)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");

            return document;
        }


        public async Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct)
        {
            var tags = await collection
                .Find(x => x.Id == funnelId)
                .Project(x => x.Tags)
                .FirstOrDefaultAsync(ct);

            if (tags is null)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");

            return tags;
        }

        public async Task<IReadOnlyList<TagDocument>> AddTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {

            var tag = new TagDocument() {
                Id = tagId,
                Name = name
            };
            
            var filter = Builders<FunnelDocument>.Filter.And(
                Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId),
                Builders<FunnelDocument>.Filter.Not(
                    Builders<FunnelDocument>.Filter.ElemMatch(x => x.Tags, t => t.Id == tag.Id)),
                Builders<FunnelDocument>.Filter.Not(
                    Builders<FunnelDocument>.Filter.ElemMatch(x => x.Tags, t => t.Name == tag.Name))
            );
            var update = Builders<FunnelDocument>.Update.Push(x => x.Tags, tag);
            var options = new FindOneAndUpdateOptions<FunnelDocument, BsonDocument>
            {
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<FunnelDocument>.Projection.Include(x => x.Tags)
            };

            var result = await collection.FindOneAndUpdateAsync<BsonDocument>(filter, update, options, ct);

            if (result is not null)
                return BsonSerializer.Deserialize<FunnelDocument>(result).Tags;

            var funnel = await collection
                .Find(x => x.Id == funnelId)
                .Project(x => new { x.Tags })
                .FirstOrDefaultAsync(ct);

            if (funnel is null)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
            if (funnel.Tags.Any(t => t.Id == tag.Id))
                throw new InvalidOperationException($"Tag with id '{tag.Id}' already exists in funnel '{funnelId}'.");
            if (funnel.Tags.Any(t => t.Name == tag.Name))
                throw new InvalidOperationException($"Tag with name '{tag.Name}' already exists in funnel '{funnelId}'.");

            throw new InvalidOperationException($"Failed to add tag to funnel '{funnelId}'.");
        }

        public async Task<IReadOnlyList<TagDocument>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.And(
                Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId),
                Builders<FunnelDocument>.Filter.ElemMatch(x => x.Tags, t => t.Id == tagId)
            );
            var update = Builders<FunnelDocument>.Update.PullFilter(x => x.Tags, t => t.Id == tagId);
            var options = new FindOneAndUpdateOptions<FunnelDocument, BsonDocument>
            {
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<FunnelDocument>.Projection.Include(x => x.Tags)
            };

            var result = await collection.FindOneAndUpdateAsync<BsonDocument>(filter, update, options, ct);

            if (result is not null)
                return BsonSerializer.Deserialize<FunnelDocument>(result).Tags;

            var exists = await collection.Find(x => x.Id == funnelId).AnyAsync(ct);
            if (!exists)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
            throw new KeyNotFoundException($"Tag with id '{tagId}' was not found in funnel '{funnelId}'.");
        }

        public async Task<IReadOnlyList<TagDocument>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.And(
                Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId),
                Builders<FunnelDocument>.Filter.ElemMatch(x => x.Tags, t => t.Id == tagId),
                Builders<FunnelDocument>.Filter.Not(
                    Builders<FunnelDocument>.Filter.ElemMatch(x => x.Tags, t => t.Name == name && t.Id != tagId))
            );
            var update = Builders<FunnelDocument>.Update.Set(x => x.Tags.FirstMatchingElement().Name, name);
            var options = new FindOneAndUpdateOptions<FunnelDocument, BsonDocument>
            {
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<FunnelDocument>.Projection.Include(x => x.Tags)
            };

            var result = await collection.FindOneAndUpdateAsync<BsonDocument>(filter, update, options, ct);

            if (result is not null)
                return BsonSerializer.Deserialize<FunnelDocument>(result).Tags;

            var funnel = await collection
                .Find(x => x.Id == funnelId)
                .Project(x => new { x.Tags })
                .FirstOrDefaultAsync(ct);

            if (funnel is null)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
            if (funnel.Tags.All(t => t.Id != tagId))
                throw new KeyNotFoundException($"Tag with id '{tagId}' was not found in funnel '{funnelId}'.");
            if (funnel.Tags.Any(t => t.Name == name && t.Id != tagId))
                throw new InvalidOperationException($"Tag with name '{name}' already exists in funnel '{funnelId}'.");

            return funnel.Tags;
        }

        public async Task<IReadOnlyCollection<FunnelDocument>> GetAllActive(CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.IsActive, true);
            return await collection.Find(filter).ToListAsync(ct);
        }

        public async Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct)
        {
            var filter = Builders<FunnelDocument>.Filter.Eq(x => x.Id, funnelId);
            var update = Builders<FunnelDocument>.Update.Set(x => x.IsActive, isActive);

            var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Funnel with id '{funnelId}' was not found.");
        }

        #endregion
    }
}
