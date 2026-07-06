using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    public sealed record SaleEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        DateTime CreatedAt,
        Guid EventId,
        LeadEventStatus Status,
        decimal DepositAmount,
        string CurrencyCode
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, CreatedAt);
}
