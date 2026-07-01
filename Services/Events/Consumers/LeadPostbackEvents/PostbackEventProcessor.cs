using System.Text.Json;
using System.Text.Json.Serialization;
using states.Mongo.Repositories;
using states.Services.CampaignService;
using states.Services.Events.Consumers.LeadPostbackEvents.Payloads;
using states.Services.LeadService;

namespace states.Services.Events.Consumers.LeadPostbackEvents;

public class PostbackEventProcessor(
    ILeadStateRepository leadStateRepository,
    ILeadProgressionService leadProgressionService,
    ICampaignClient campaignClient,
    ILogger<PostbackEventProcessor> logger) : IPostbackEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task Process(string eventType, string rawPayload, CancellationToken ct)
    {
        if (!Enum.TryParse<PostbackEventType>(eventType, ignoreCase: true, out var postbackEventType))
        {
            logger.LogWarning("Unknown postback event type '{EventType}', skipping", eventType);
            return;
        }

        PostbackEventPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PostbackEventPayload>(rawPayload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize postback event '{EventType}'", eventType);
            return;
        }

        if (payload is null)
        {
            logger.LogWarning("Received null payload for postback event '{EventType}'", eventType);
            return;
        }

        var leadStates = await leadStateRepository.GetLeadStatesByLeadId(payload.TenantId, payload.LeadId, ct);
        if (leadStates.Count == 0)
        {
            logger.LogWarning(
                "Lead '{LeadId}' not found for tenant {TenantId}, skipping postback {EventType}",
                payload.LeadId, payload.TenantId, postbackEventType);
            return;
        }

        if (leadStates.Count > 1)
        {
            // Кампания может вести лида через несколько ботов — пока обрабатываем только первый
            // найденный leadState, остальные боты этого лида постбэк не получат.
            logger.LogWarning(
                "Lead '{LeadId}' for tenant {TenantId} matched {Count} lead states, processing only the first one",
                payload.LeadId, payload.TenantId, leadStates.Count);
        }

        var leadState = leadStates[0];

        if (payload.CustomFields is { Count: > 0 })
            await leadStateRepository.MergePostbackParameters(payload.TenantId, payload.LeadId, payload.CustomFields, ct);

        if (leadState.CampaignId is null)
        {
            logger.LogInformation(
                "Lead '{LeadId}' has no campaign, skipping postback {EventType}",
                payload.LeadId, postbackEventType);
            return;
        }

        var entryPoint = await campaignClient.GetAutoActionEntryPoint(
            payload.TenantId, leadState.CampaignId.Value, postbackEventType, ct);

        if (entryPoint is null)
        {
            logger.LogInformation(
                "No auto action configured for campaign {CampaignId}, postback {EventType} — skipping",
                leadState.CampaignId, postbackEventType);
            return;
        }

        await leadProgressionService.SetLeadFunnelPosition(
            payload.TenantId,
            payload.LeadId,
            entryPoint.FunnelId,
            entryPoint.FlowId,
            entryPoint.NodeId,
            ct);

        logger.LogInformation(
            "Lead '{LeadId}' moved to funnel {FunnelId} flow {FlowId} node {NodeId} by postback {EventType}",
            payload.LeadId, entryPoint.FunnelId, entryPoint.FlowId, entryPoint.NodeId, postbackEventType);
    }
}
