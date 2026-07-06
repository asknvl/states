using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public abstract record LeadEventBaseDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt);
}
