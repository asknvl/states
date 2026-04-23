namespace states.Dtos.Leads;

public sealed record EnterFunnelRequest(
    Guid TenantId,    
    Guid SpaceId,
    Guid BotId,
    Guid ChatId,
    string LeadId,
    Guid CampaignId,
    string CampaignName,
    string? SourceId,
    string? SourceName,
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId
);
