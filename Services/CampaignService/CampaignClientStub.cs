namespace states.Services.CampaignService;

// TODO: replace with real HTTP client to campaign service
public class CampaignClientStub : ICampaignClient
{
    public Task<FunnelEntryPoint?> GetFunnelEntryPoint(Guid tenantId, Guid botId, CancellationToken ct)
        => Task.FromResult<FunnelEntryPoint?>(null);
}
