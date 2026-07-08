using MongoDB.Driver;
using states.Mongo.Documents.LeadEvents;
using states.Services.LeadEventsService.Application;

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
            IReadOnlyCollection<LeadEventTypes>? eventTypes,
            CancellationToken ct = default)
        {
            var filter = Builders<LeadEventBaseDocument>.Filter.And(
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.SpaceId, spaceId),
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.LeadId, leadId));

            if (eventTypes is { Count: > 0 })
            {
                var typeFilters = eventTypes
                    .Select(BuildTypeFilter)
                    .OfType<FilterDefinition<LeadEventBaseDocument>>()
                    .ToList();

                if (typeFilters.Count == 0)
                {
                    return Array.Empty<LeadEventBaseDocument>();
                }

                filter = Builders<LeadEventBaseDocument>.Filter.And(filter, Builders<LeadEventBaseDocument>.Filter.Or(typeFilters));
            }

            return await collection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        private static FilterDefinition<LeadEventBaseDocument>? BuildTypeFilter(LeadEventTypes type)
        {
            return type switch
            {
                LeadEventTypes.BotActivation => Builders<LeadEventBaseDocument>.Filter.OfType<BotActivationEventDocument>(),
                LeadEventTypes.BotDeactivation => Builders<LeadEventBaseDocument>.Filter.OfType<BotDeactivationEventDocument>(),
                LeadEventTypes.Contact => Builders<LeadEventBaseDocument>.Filter.OfType<ContactEventDocument>(),
                LeadEventTypes.ChannelSubscribtion => Builders<LeadEventBaseDocument>.Filter.OfType<ChannelSubscriptionEventDocument>(),
                LeadEventTypes.Registration => Builders<LeadEventBaseDocument>.Filter.OfType<RegistrationLeadEvent>(),
                LeadEventTypes.Sale => Builders<LeadEventBaseDocument>.Filter.OfType<SaleLeadEvent>(),
                LeadEventTypes.Resale => Builders<LeadEventBaseDocument>.Filter.OfType<ResaleLeadEvent>(),
                _ => null
            };
        }

        public async Task Create(LeadEventBaseDocument document, CancellationToken ct = default)
        {
            await collection.InsertOneAsync(document, cancellationToken: ct);
        }
    }
}
