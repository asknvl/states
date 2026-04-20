using states.Services.LeadService;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadStatusChangedPayload(        
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        LeadFunnelStatus Status,
        long Version
    ) : LeadStateChangeEventPayloadBase(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            LeadId: LeadId,
            Version: Version);
}
