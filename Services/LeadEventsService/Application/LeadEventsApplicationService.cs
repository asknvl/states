using states.Dtos.LeadEvents;
using states.Mongo.Repositories;
using states.Services.LeadEventsService.Mapping;

namespace states.Services.LeadEventsService.Application
{
    public class LeadEventsApplicationService(
        ILeadEventsRepository leadEventsRepository) : ILeadEventsApplicationService
    {
        public async Task<IReadOnlyCollection<LeadEventBaseDto>> GetLeadEvents(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes = null,
            CancellationToken ct = default)
        {
            var documents = await leadEventsRepository.GetByLead(tenantId, spaceId, leadId, eventTypes, ct);
            return documents.Select(d => d.ToDto()).ToList();
        }

        public IReadOnlyCollection<LeadEventTypes> GetLeadEventTypes()
        {
            return Enum.GetValues<LeadEventTypes>();
        }
    }
}
