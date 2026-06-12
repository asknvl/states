namespace states.Services.CampaignService;

public record EntryPointDto(
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId
);
