using states.Services.Events.Consumer;

namespace states.Services.CampaignService;

public interface ICampaignClient
{
    /// <summary>
    /// Returns the funnel entry point configured for the given bot,
    /// or null if no active campaign is found.
    /// </summary>
    Task<FunnelEntryPoint> GetFunnelEntryPoint(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        string? startParameter,
        CancellationToken ct);

    /// <summary>
    /// Returns the funnel entry point configured as an auto action for the given
    /// campaign and postback event type, or null if no such auto action is configured.
    /// </summary>
    Task<EntryPointDto?> GetAutoActionEntryPoint(
        Guid tenantId,
        Guid campaignId,
        PostbackEventType postbackEventType,
        CancellationToken ct);
}
