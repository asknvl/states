using states.Services.LeadService;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadStatusChangedPayload(
        Guid LeadStateId,
        Guid TenantId,
        Guid ChatId,
        LeadFunnelStatus Status,
        long Version
    );
}
