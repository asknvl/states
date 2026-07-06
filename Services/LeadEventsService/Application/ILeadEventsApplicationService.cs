using states.Dtos.LeadEvents;

namespace states.Services.LeadEventsService.Application
{
    public interface ILeadEventsApplicationService
    {
        Task<IReadOnlyCollection<LeadEventBaseDto>> GetLeadEvents(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            CancellationToken ct = default);
    }
}
