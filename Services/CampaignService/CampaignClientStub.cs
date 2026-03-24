namespace states.Services.CampaignService;

public class CampaignClientStub : ICampaignClient
{
    public async Task<FunnelEntryPoint?> GetFunnelEntryPoint(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        string? startParameter,
        CancellationToken ct)
    {

        await Task.CompletedTask;

        return new FunnelEntryPoint(
            FunnelId: new Guid("019d1b99-da04-70ca-8620-cc2e7b316415"),
            FlowId: new Guid("0195930e-84a4-72c9-8f3b-6a1d4e7c0b95"),
            NodeId: new Guid("0195930e-84a5-7d37-a8b1-5c9e2f6d4a10"));
    }
}