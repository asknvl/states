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
        string CampaignName,
        string SourceId,
        string SourceName,
        Guid FunnelId,
        string FunnelName,
        Guid FlowId,
        string FlowName,
        Guid NodeId,
        string NodeLabel,
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
