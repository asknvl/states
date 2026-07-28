using System.Text.Json;
using System.Text.Json.Serialization;
using states.Dtos.Leads;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Repositories;
using states.Services.CampaignService;
using states.Services.Events.Consumer;
using states.Services.Events.Consumers.GlobalEvents.Payloads;
using states.Services.LeadService;
using states.Services.MigratorService;

namespace states.Services.Events.Consumers.GlobalEvents;

public class GlobalEventProcessor(
    ILeadProgressionService leadProgressionService,
    ICampaignClient campaignClient,
    IMigratorClient migratorClient,
    ILeadStateRepository leadStateRepository,
    ILeadEventsRepository leadEventsRepository,
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
            case EventTypes.BotActivationStatusChanged:
                await HandleSubscriptionChanged(rawPayload, ct);
                break;

            case EventTypes.IncomingMessageSignal:
                await HandleIncomingMessageSignal(rawPayload, ct);
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
            logger.LogError(ex, "Failed to deserialize {EventType}", EventTypes.BotActivationStatusChanged);
            return;
        }

        if (incoming is null)
        {
            logger.LogWarning("Received null payload for {EventType}", EventTypes.BotActivationStatusChanged);
            return;
        }

        var p = incoming.Payload;        

        if (!p.IsActive)
        {
            logger.LogInformation(
                "Bot subscription deactivated for chat {ChatId}, bot {BotId} — marking lead as blocked",
                p.ChatId, p.BotId);

            var existingLeadState = await leadStateRepository.GetLeadStateByChatId(p.TenantId, p.ChatId, ct);
            if (existingLeadState is not null)
            {
                await leadEventsRepository.Create(new BotDeactivationEventDocument
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = p.TenantId,
                    SpaceId = p.SpaceId,
                    LeadId = existingLeadState.LeadId,
                    CreatedAt = DateTime.UtcNow,
                    BotId = p.BotId
                }, ct);
            }

            await leadProgressionService.MarkLeadBlocked(p.TenantId, p.ChatId, ct);
            return;
        }

        FunnelEntryPoint entryPoint = null!;


        try
        {

            entryPoint = await campaignClient.GetFunnelEntryPoint(
                p.TenantId,
                p.BotId,
                p.GlobalId,
                p.StartParameter,
                ct);

        } catch (Exception ex)
        {

        }

        EnterFunnelRequest request = null!;

        if (entryPoint is null)
        {
            logger.LogError(
                "No lead entry point for tenant {TenantId}, bot {BotId} — cannot enter funnel",
                p.TenantId, p.BotId);

            return;
        }
        else
        {

            request = new EnterFunnelRequest(
                TenantId: p.TenantId,
                SpaceId: p.SpaceId,
                BotId: p.BotId,
                ChatId: p.ChatId,
                LeadId: entryPoint.LeadId,
                CampaignId: entryPoint.CampaignId,
                CampaignName: entryPoint.CampaignName,
                SourceId: entryPoint.SourceId,
                SourceName: entryPoint.SourceName,
                FunnelId: entryPoint.FunnelId,
                FlowId: entryPoint.FlowId,
                NodeId: entryPoint.NodeId,

                StartParameter: p.StartParameter,

                MigrationFrom: entryPoint.MigrationFrom,

                Tags: await GetMigratedLeadTags(entryPoint, p, ct));
        }

        await leadEventsRepository.Create(new BotActivationEventDocument
        {
            Id = Guid.CreateVersion7(),
            TenantId = p.TenantId,
            SpaceId = p.SpaceId,
            LeadId = entryPoint.LeadId,
            CreatedAt = DateTime.UtcNow,
            BotId = p.BotId
        }, ct);

        await leadProgressionService.EnterFunnel(request, ct);

    }

    /// <summary>
    /// Теги мигрированного лида из migrator — уже переведённые в наши идентификаторы
    /// по соответствиям «было — стало». Запрашиваются только когда campaigns пометил
    /// вход флагом миграции. Недоступность migrator лида не теряет — он войдёт без тегов.
    /// </summary>
    private async Task<List<Dtos.Funnels.Tag>?> GetMigratedLeadTags(
        FunnelEntryPoint entryPoint,
        BotSubscriptionChangedPayload p,
        CancellationToken ct)
    {
        if (entryPoint.MigrationFrom is null or states.Services.CampaignClient.MigrationFrom.None)
            return null;

        try
        {
            var tags = await migratorClient.GetMigratedLeadTags(p.TenantId, p.BotId, p.GlobalId, ct);

            if (tags is null)
            {
                logger.LogInformation(
                    "Migrated lead {GlobalId} of bot {BotId} is unknown to migrator, entering funnel without tags",
                    p.GlobalId, p.BotId);
                return null;
            }

            logger.LogInformation(
                "Migrated lead {GlobalId} of bot {BotId} gets {TagCount} tag(s) from migrator",
                p.GlobalId, p.BotId, tags.Count);

            return tags.Select(t => new Dtos.Funnels.Tag(t.TagId, t.Name)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Migrated lead tags lookup failed for GlobalId={GlobalId}, entering funnel without tags",
                p.GlobalId);
            return null;
        }
    }

    private async Task HandleIncomingMessageSignal(string rawPayload, CancellationToken ct)
    {
        IncomingEvent<IncomingMessageSignalPayload>? incoming;
        try
        {
            incoming = JsonSerializer.Deserialize<IncomingEvent<IncomingMessageSignalPayload>>(rawPayload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize {EventType}", EventTypes.IncomingMessageSignal);
            return;
        }

        if (incoming is null)
        {
            logger.LogWarning("Received null payload for {EventType}", EventTypes.IncomingMessageSignal);
            return;
        }

        var p = incoming.Payload;

        // Первое входящее сообщение лида — событие Contact. TryMarkFirstContact атомарно
        // выставляет FirstContactAt и возвращает документ только один раз, повторные сигналы дают null.
        var firstContact = await leadStateRepository.TryMarkFirstContact(p.TenantId, p.BotId, p.ChatId, ct);
        if (firstContact is not null)
        {
            await leadEventsRepository.Create(new ContactEventDocument
            {
                Id = Guid.CreateVersion7(),
                TenantId = firstContact.TenantId,
                SpaceId = firstContact.SpaceId,
                LeadId = firstContact.LeadId,
                CreatedAt = firstContact.FirstContactAt ?? DateTime.UtcNow
            }, ct);
        }

        await leadProgressionService.HandleIncomingSignal(p.TenantId, p.BotId, p.ChatId, ct);
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

