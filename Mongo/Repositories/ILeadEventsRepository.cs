using states.Mongo.Documents.LeadEvents;
using states.Services.LeadEventsService.Application;

namespace states.Mongo.Repositories
{
    public interface ILeadEventsRepository
    {
        Task<IReadOnlyCollection<LeadEventBaseDocument>> GetByLead(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes = null,
            CancellationToken ct = default);

        Task Create(LeadEventBaseDocument document, CancellationToken ct = default);
    }
}
