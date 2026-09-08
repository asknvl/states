using MongoDB.Driver;
using states.Mongo.Documents.LeadEvents;
using states.Services.LeadEventsService.Application;

namespace states.Mongo.Repositories
{
    public class LeadEventsRepository : ILeadEventsRepository
    {
        private readonly IMongoCollection<LeadEventBaseDocument> collection;
        private readonly ILogger<LeadEventsRepository> logger;

        public LeadEventsRepository(MongoContext context, ILogger<LeadEventsRepository> logger)
        {
            collection = context.LeadEvents;
            this.logger = logger;
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

        public async Task<long> DeleteByLead(Guid tenantId, string leadId, CancellationToken ct = default)
        {
            var filter = Builders<LeadEventBaseDocument>.Filter.And(
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.TenantId, tenantId),
                Builders<LeadEventBaseDocument>.Filter.Eq(x => x.LeadId, leadId));

            var result = await collection.DeleteManyAsync(filter, ct);
            return result.DeletedCount;
        }

        public async Task<IReadOnlyList<LeadEventBaseDocument>> CreateMany(
            IReadOnlyList<LeadEventBaseDocument> documents,
            CancellationToken ct = default)
        {
            if (documents.Count == 0)
                return [];

            try
            {
                await collection.InsertManyAsync(documents, new InsertManyOptions { IsOrdered = false }, ct);
                return documents;
            }
            catch (MongoBulkWriteException<LeadEventBaseDocument> ex)
            {
                // unordered — часть пачки успешно вставилась, часть отклонена уникальным индексом
                // (tenantId, eventId) на повторном/резюмированном прогоне миграции. WriteErrors.Index —
                // индекс запроса в исходном списке (гарантия драйвера), по нему и вычитаем неудачные.
                var duplicates = ex.WriteErrors.Count(e => e.Category == ServerErrorCategory.DuplicateKey);
                if (duplicates != ex.WriteErrors.Count)
                    logger.LogWarning(
                        "CreateMany: {Total} write error(s), {Duplicates} duplicate key, {Other} other — " +
                        "the non-duplicate ones are silently dropped, not retried",
                        ex.WriteErrors.Count, duplicates, ex.WriteErrors.Count - duplicates);

                var failedIndexes = ex.WriteErrors.Select(e => e.Index).ToHashSet();
                return documents.Where((_, i) => !failedIndexes.Contains(i)).ToList();
            }
        }
    }
}
