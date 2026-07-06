using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record ResaleEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt,
        decimal DepositAmount,
        string CurrencyCode
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, EventId, Status, CreatedAt);
}
