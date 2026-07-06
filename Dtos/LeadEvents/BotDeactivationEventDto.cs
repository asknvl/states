using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record BotDeactivationEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt,
        Guid BotId
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, EventId, Status, CreatedAt);
}
