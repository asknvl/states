using System.Text.Json;
using System.Text.Json.Serialization;
using states.Dtos.Leads;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Repositories;
using states.Services.CampaignService;
using states.Mongo.Documents.Outbox;
using states.Services.Events.Consumer;
using states.Services.Events.Consumers.GlobalEvents.Payloads;
using states.Services.Events.Producer.Payloads.Conversions;
using states.Services.LeadService;
using states.Services.MigratorService;

namespace states.Services.Events.Consumers.GlobalEvents;

public class GlobalEventProcessor(
    ILeadProgressionService leadProgressionService,
    ICampaignClient campaignClient,
    IMigratorClient migratorClient,
    ILeadStateRepository leadStateRepository,
    ILeadEventsRepository leadEventsRepository,
    IOutboxRepository outboxRepository,
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

            var blocked = await leadProgressionService.MarkLeadBlocked(p.TenantId, p.ChatId, ct);

            // tgengine шлёт деактивацию на каждый отказ отправки — лид-событие пишем
            // только по реально заблокированным, повторные события дублей не плодят.
            foreach (var leadState in blocked)
            {
                await leadEventsRepository.Create(new BotDeactivationEventDocument
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = p.TenantId,
                    SpaceId = p.SpaceId,
                    LeadId = leadState.LeadId,
                    CreatedAt = DateTime.UtcNow,
                    BotId = p.BotId
                }, ct);
            }

            return;
        }

        logger.LogInformation(
            "Bot subscription activated: chat {ChatId}, bot {BotId}, global {GlobalId}, start parameter '{StartParameter}' — requesting funnel entry point",
            p.ChatId, p.BotId, p.GlobalId, p.StartParameter);

        FunnelEntryPoint? entryPoint = null;

        try
        {
            entryPoint = await campaignClient.GetFunnelEntryPoint(
                p.TenantId,
                p.BotId,
                p.GlobalId,
                p.StartParameter,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Funnel entry point request failed for tenant {TenantId}, bot {BotId}, global {GlobalId}, start parameter '{StartParameter}'",
                p.TenantId, p.BotId, p.GlobalId, p.StartParameter);
        }

        EnterFunnelRequest request = null!;

        if (entryPoint is null)
        {
            logger.LogError(
                "No lead entry point for tenant {TenantId}, bot {BotId}, global {GlobalId}, start parameter '{StartParameter}' — cannot enter funnel",
                p.TenantId, p.BotId, p.GlobalId, p.StartParameter);

            return;
        }
        else
        {
            logger.LogInformation(
                "Funnel entry point resolved: lead {LeadId}, campaign {CampaignId} ('{CampaignName}'), source {SourceId} ('{SourceName}'), funnel {FunnelId}, node {NodeId}, migration {MigrationFrom}",
                entryPoint.LeadId, entryPoint.CampaignId, entryPoint.CampaignName,
                entryPoint.SourceId, entryPoint.SourceName,
                entryPoint.FunnelId, entryPoint.NodeId, entryPoint.MigrationFrom);

            // Ходим в migrator только за лидами тех кампаний, которые принимают мигрированных:
            // для остальных запроса нет вообще.
            //
            var migrated = entryPoint.MigrationFrom is not null and not states.Services.CampaignClient.MigrationFrom.None
                ? await GetMigratedLeadState(p, ct)
                : null;

            request = new EnterFunnelRequest(
                TenantId: p.TenantId,
                SpaceId: p.SpaceId,
                BotId: p.BotId,
                ChatId: p.ChatId,
                ExternalId: p.ExternalId,
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

                Tags: migrated?.Tags.Select(t => new Dtos.Funnels.Tag(t.TagId, t.Name)).ToList(),
                Status: migrated?.Status);
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
    /// Статус и теги мигрированного лида из migrator. Недоступность migrator лида не теряет —
    /// он войдёт как обычный, со статусом от ноды входа и без перенесённых тегов.
    /// 
    /// </summary>
    /// 
    private async Task<MigratedLeadState?> GetMigratedLeadState(
        BotSubscriptionChangedPayload p,
        CancellationToken ct)
    {
        try
        {
            var migrated = await migratorClient.GetMigratedLeadState(p.TenantId, p.BotId, p.GlobalId, ct);

            if (migrated is null)
            {
                logger.LogInformation(
                    "Migrated lead {GlobalId} of bot {BotId} is unknown to migrator, entering funnel as a regular one",
                    p.GlobalId, p.BotId);
                return null;
            }

            logger.LogInformation(
                "Migrated lead {GlobalId} of bot {BotId} enters with status {Status} and {TagCount} tag(s)",
                p.GlobalId, p.BotId, migrated.Status, migrated.Tags.Count);

            return migrated;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Migrated lead state lookup failed for GlobalId={GlobalId}, entering funnel as a regular one",
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
            // Конверсия «контакт» для ФБ-пайплайна — в outbox первым делом: конверсия важнее
            // журнальной записи ниже. Id = Id лид-стейта (TryMarkFirstContact срабатывает
            // ровно один раз, id детерминирован — дедуп ниже по конвейеру). Органика (без
            // кампании) в ФБ не отправляется. Доставкой занимается OutboxWorkerService.
            if (firstContact.CampaignId is not null)
                await outboxRepository.TryAdd(new LeadConversionOutboxDocument
                {
                    Id = firstContact.Id,
                    CreatedAt = DateTime.UtcNow,
                    TenantId = firstContact.TenantId,
                    SpaceId = firstContact.SpaceId,
                    BotId = firstContact.BotId,
                    ChatId = firstContact.ChatId,
                    LeadId = firstContact.LeadId,
                    ConversionType = LeadConversionType.Contact,
                    CampaignId = firstContact.CampaignId.Value,
                    OccurredAt = firstContact.FirstContactAt ?? DateTime.UtcNow,
                }, ct);

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

