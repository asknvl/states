using MongoDB.Driver;
using states.Mongo.Documents.TenantTags;

namespace states.Mongo.Repositories
{
    public class TenantTagsRepository(MongoContext context) : ITenantTagsRepository
    {
        private readonly IMongoCollection<TenantTag> collection = context.TenantTags;

        public async Task<(Guid tagId, string name)> CreateIfNeed(Guid tenantId, Guid spaceId, Guid funnelId, string tagName)
        {
            // Шаг 1: гарантируем единственный документ по (tenantId, tagName).
            var nameFilter = Builders<TenantTag>.Filter.And(
                Builders<TenantTag>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<TenantTag>.Filter.Eq(x => x.TagName, tagName)
            );

            var upsertUpdate = Builders<TenantTag>.Update
                .SetOnInsert(x => x.Id, Guid.CreateVersion7())
                .SetOnInsert(x => x.TagId, Guid.CreateVersion7())
                .SetOnInsert(x => x.TenantId, tenantId)
                .SetOnInsert(x => x.TagName, tagName);

            var doc = await collection.FindOneAndUpdateAsync(nameFilter, upsertUpdate,
                new FindOneAndUpdateOptions<TenantTag> { IsUpsert = true, ReturnDocument = ReturnDocument.After });

            // Шаг 2: добавляем Usage(spaceId, funnelId) если такой пары ещё нет.
            var alreadyRegistered = doc.Usages.Any(u => u.SpaceId == spaceId && u.FunnelId == funnelId);
            if (!alreadyRegistered)
            {
                var usageFilter = Builders<TenantTag>.Filter.And(
                    Builders<TenantTag>.Filter.Eq(x => x.Id, doc.Id),
                    Builders<TenantTag>.Filter.Not(
                        Builders<TenantTag>.Filter.ElemMatch(x => x.Usages,
                            Builders<Usage>.Filter.And(
                                Builders<Usage>.Filter.Eq(u => u.SpaceId, spaceId),
                                Builders<Usage>.Filter.Eq(u => u.FunnelId, funnelId))))
                );

                await collection.UpdateOneAsync(usageFilter,
                    Builders<TenantTag>.Update.Push(x => x.Usages, new Usage { SpaceId = spaceId, FunnelId = funnelId }));
            }

            return (doc.TagId, doc.TagName);
        }

        public async Task<IReadOnlyCollection<TenantTag>> GetTenantTags(Guid tenantId, Guid spaceId)
        {
            var filter = Builders<TenantTag>.Filter.And(
                Builders<TenantTag>.Filter.Eq(x => x.TenantId, tenantId)
                //Builders<TenantTag>.Filter.ElemMatch(x => x.Usages,
                //    Builders<Usage>.Filter.Eq(u => u.SpaceId, spaceId))
            );

            return await collection.Find(filter).ToListAsync();
        }

        public async Task UpdateTenantTagName(Guid tagId, string name)
        {
            var filter = Builders<TenantTag>.Filter.Eq(x => x.TagId, tagId);
            var update = Builders<TenantTag>.Update.Set(x => x.TagName, name);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task DecreaseTenantTagUsage(Guid tagId, Guid funnelId)
        {
            var filter = Builders<TenantTag>.Filter.Eq(x => x.TagId, tagId);
            var update = Builders<TenantTag>.Update.PullFilter(x => x.Usages,
                Builders<Usage>.Filter.Eq(u => u.FunnelId, funnelId));
            await collection.UpdateOneAsync(filter, update);
        }
    }
}
