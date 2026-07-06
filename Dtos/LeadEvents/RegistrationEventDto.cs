using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record RegistrationEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        DateTime CreatedAt,
        Guid EventId,
        LeadEventStatus Status,
        string CurrencyCode
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, CreatedAt);
}
