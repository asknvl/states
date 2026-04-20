using states.Services.LeadService;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadStateCreatedPayload(        
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        Guid CampaignId,        
        Guid FunnelId,
        Guid FlowId,
        Guid NodeId,
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
