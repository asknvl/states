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
            // $setOnInsert не трогает существующий документ — просто возвращает его.
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

            // Шаг 2: регистрируем воронку, если она ещё не использует этот тег.
            // Фильтр по Id исключает повторный $inc при конкурентных запросах.
            if (!doc.UsedInFunnels.Contains(funnelId))
            {
                var usageFilter = Builders<TenantTag>.Filter.And(
                    Builders<TenantTag>.Filter.Eq(x => x.Id, doc.Id),
                    Builders<TenantTag>.Filter.Not(
                        Builders<TenantTag>.Filter.AnyEq(x => x.UsedInFunnels, funnelId))
                );

                await collection.UpdateOneAsync(usageFilter,
                    Builders<TenantTag>.Update
                        .Inc(x => x.UsagesNumber, 1)
                        .AddToSet(x => x.UsedInFunnels, funnelId)
                        .AddToSet(x => x.UsedInSpaces, spaceId));
            }

            return (doc.TagId, doc.TagName);
        }

        public async Task<IReadOnlyCollection<TenantTag>> GetTenantTags(Guid tenantId, Guid spaceId)
        {
            var filter = Builders<TenantTag>.Filter.And(
                Builders<TenantTag>.Filter.Eq(x => x.TenantId, tenantId)
                //Builders<TenantTag>.Filter.AnyEq(x => x.UsedInSpaces, spaceId) //TODO ����� ����� ���������� ���� ������� ������ �� ������ �������, � ������� ����?
            );

            return await collection.Find(filter).ToListAsync();
        }
    }
}
