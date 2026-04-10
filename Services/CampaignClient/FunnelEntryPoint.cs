namespace states.Services.CampaignService;

public record FunnelEntryPoint(
    string LeadId,
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId
);
