namespace states.Services.CampaignService;

public interface ICampaignClient
{
    /// <summary>
    /// Returns the funnel entry point configured for the given bot,
    /// or null if no active campaign is found.
    /// </summary>
    Task<FunnelEntryPoint?> GetFunnelEntryPoint(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        string? startParameter,
        CancellationToken ct);
}
