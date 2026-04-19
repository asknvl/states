using states.Services.LeadService;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadStateCreatedPayload(
        Guid LeadStateId,
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        Guid CampaignId,
        string LeadId,
        Guid FunnelId,
        Guid FlowId,
        Guid NodeId,
        LeadFunnelStatus Status,
        long Version
    );
}
