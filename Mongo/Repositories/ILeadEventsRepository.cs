using states.Mongo.Documents.LeadEvents;

namespace states.Mongo.Repositories
{
    public interface ILeadEventsRepository
    {
        Task<IReadOnlyCollection<LeadEventBaseDocument>> GetByLead(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            CancellationToken ct = default);

        Task Create(LeadEventBaseDocument document, CancellationToken ct = default);
    }
}
