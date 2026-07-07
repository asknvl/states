using states.Dtos.LeadEvents;

namespace states.Services.LeadEventsService.Application
{
    public interface ILeadEventsApplicationService
    {
        Task<IReadOnlyCollection<LeadEventBaseDto>> GetLeadEvents(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes = null,
            CancellationToken ct = default);

        IReadOnlyCollection<LeadEventTypes> GetLeadEventTypes();
    }
}
