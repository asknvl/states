using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record RegistrationEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt,
        string CurrencyCode
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, EventId, Status, CreatedAt);
}
