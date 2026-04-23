namespace states.Services.CampaignService;

public record FunnelEntryPoint(
    string LeadId,
    Guid CampaignId,
    string CampaignName,
    string? SourceId,
    string? SourceName,
    Guid FunnelId,   
    Guid FlowId,
    Guid NodeId
);
