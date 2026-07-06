using MongoDB.Driver;
using states.Mongo.Documents.LeadEvents;

namespace states.Mongo.Repositories
{
    public class LeadEventsRepository : ILeadEventsRepository
    {
        private readonly IMongoCollection<LeadEventBaseDocument> collection;

        public LeadEventsRepository(MongoContext context)
        {
            collection = context.LeadEvents;
        }

        public async Task<IReadOnlyCollection<LeadEventBaseDocument>> GetByLead(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            CancellationToken ct = default)
        {
            var filter = Builders<LeadEventBaseDocument>.Filter.And(
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.SpaceId, spaceId),
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.LeadId, leadId));

            return await collection
                .Find(filter)
                .SortBy(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task Create(LeadEventBaseDocument document, CancellationToken ct = default)
        {
            await collection.InsertOneAsync(document, cancellationToken: ct);
        }
    }
}
