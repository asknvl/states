using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record ContactEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, EventId, Status, CreatedAt);
}
