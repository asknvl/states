using System.Text.Json;
using System.Text.Json.Serialization;
using states.Dtos.Leads;
using states.Services.CampaignService;
using states.Services.Events.Payloads;
using states.Services.LeadService;

namespace states.Services.Events.Consumer;

public class GlobalEventProcessor(
    ILeadProgressionService leadProgressionService,
    ICampaignClient campaignClient,
    ILogger<GlobalEventProcessor> logger) : IGlobalEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task Process(string eventType, string rawPayload, CancellationToken ct)
    {
        switch (eventType)
        {
            case EventTypes.SubscriptionBotStatusChanged:
                await HandleSubscriptionChanged(rawPayload, ct);
                break;

            case EventTypes.ChatDeleted:
                await HandleChatDeletion(rawPayload, ct);
                break;

            default:
                logger.LogDebug("Unhandled event type '{EventType}', skipping", eventType);
                break;
        }
    }

    private async Task HandleSubscriptionChanged(string rawPayload, CancellationToken ct)
    {
        IncomingEvent<BotSubscriptionChangedPayload>? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<IncomingEvent<BotSubscriptionChangedPayload>>(rawPayload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize {EventType}", EventTypes.SubscriptionBotStatusChanged);
            return;
        }

        if (incoming is null)
        {
            logger.LogWarning("Received null payload for {EventType}", EventTypes.SubscriptionBotStatusChanged);
            return;
        }

        var p = incoming.Payload;        

        if (!p.IsActive)
        {
            logger.LogInformation(
                "Bot subscription deactivated for chat {ChatId}, bot {BotId} — skipping",
                p.ChatId, p.BotId);
            return;
        }

        var entryPoint = await campaignClient.GetFunnelEntryPoint(
            p.TenantId,
            p.BotId,
            p.GlobalId,
            p.StartParameter,
            ct);

        if (entryPoint is null)
        {
            logger.LogWarning(
                "No active campaign found for tenant {TenantId}, bot {BotId} — cannot enter funnel",
                p.TenantId, p.BotId);
            return;
        }

        var request = new EnterFunnelRequest(
            TenantId: p.TenantId,            
            BotId: p.BotId,
            ChatId: p.ChatId,
            LeadId: entryPoint.LeadId,
            FunnelId: entryPoint.FunnelId,
            FlowId: entryPoint.FlowId,
            NodeId: entryPoint.NodeId);

        await leadProgressionService.EnterFunnel(request, ct);
    }

    private async Task HandleChatDeletion(string rawPayload, CancellationToken ct)
    {
        IncomingEvent<ChatDeletionPayload>? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<IncomingEvent<ChatDeletionPayload>>(rawPayload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize {EventType}", EventTypes.ChatDeleted);
            return;
        }

        if (incoming is null)
        {
            logger.LogWarning("Received null payload for {EventType}", EventTypes.ChatDeleted);
            return;
        }

        var p = incoming.Payload;

        await leadProgressionService.ClearLeadStateByChat(p.TenantId, p.ChatId);        
    }
}

