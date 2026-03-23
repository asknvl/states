namespace states.Services.CampaignService;

public class CampaignClientStub : ICampaignClient
{
    public Task<FunnelEntryPoint?> GetFunnelEntryPoint(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        string? startParameter,
        CancellationToken ct) => Task.FromResult<FunnelEntryPoint?>(null);
}