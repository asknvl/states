using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record GetLeadEventsRequest(
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        IReadOnlyCollection<LeadEventTypes> EventTypes
    );
}
