namespace states.Services.CampaignService;

public record FunnelEntryPoint(
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId
);
