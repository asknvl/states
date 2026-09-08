using System.Text.Json;
using System.Text.Json.Serialization;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Repositories;
using states.Mongo.Documents.Outbox;
using states.Services.CampaignService;
using states.Services.Events.Consumers.LeadPostbackEvents.Payloads;
using states.Services.Events.Producer.Payloads.Conversions;
using states.Services.LeadEventsService.Application;
using states.Services.LeadService;

namespace states.Services.Events.Consumers.LeadPostbackEvents;

public class PostbackEventProcessor(
    ILeadStateRepository leadStateRepository,
    ILeadEventsRepository leadEventsRepository,
    ILeadProgressionService leadProgressionService,
    ICampaignClient campaignClient,
    IOutboxRepository outboxRepository,
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
            // Кампания может вести лида через несколько ботов. AutoActions пока актуальны только
            // для самого первого состояния лида (создано при входе лида в воронку) — репозиторий
            // возвращает состояния отсортированными по createdAt, так что [0] — именно оно.
            logger.LogWarning(
                "Lead '{LeadId}' for tenant {TenantId} matched {Count} lead states, processing only the earliest one",
                payload.LeadId, payload.TenantId, leadStates.Count);
        }

        var leadState = leadStates[0];

        if (payload.CustomFields is { Count: > 0 })
            await leadStateRepository.MergePostbackParameters(payload.TenantId, payload.LeadId, payload.CustomFields, ct);

        // Конверсия для ФБ-пайплайна — в outbox ДО записи в lead_events: при переигрывании
        // постбэка Create ниже упадёт на unique-индексе (tenantId, eventId) и обработка
        // оборвётся, а конверсия к этому моменту уже гарантированно стоит в очереди.
        // Вставка идемпотентна: Id документа = EventId постбэка в трекере.
        // Доставкой занимается OutboxWorkerService — конверсия переживает простой Kafka.
        // Конверсию рождает только ACCEPTED: DUPLICATE — повторная доставка того же события,
        // REPEAT — повторное событие лида (у него свой eventId, идемпотентность по Id не спасёт) —
        // оба в ФБ не шлём, иначе задвоение конверсий на пикселе.
        var conversionType = postbackEventType switch
        {
            PostbackEventType.REGISTRATION => LeadConversionType.Registration,
            PostbackEventType.SALE => LeadConversionType.Sale,
            PostbackEventType.RESALE => LeadConversionType.Resale,
            _ => (LeadConversionType?)null
        };

        if (conversionType is not null
            && payload.Status == PostbackEventStatus.ACCEPTED
            && leadState.CampaignId is not null)
        {
            await outboxRepository.TryAdd(new LeadConversionOutboxDocument
            {
                Id = payload.EventId,
                CreatedAt = DateTime.UtcNow,
                TenantId = payload.TenantId,
                SpaceId = leadState.SpaceId,
                BotId = leadState.BotId,
                ChatId = leadState.ChatId,
                LeadId = payload.LeadId,
                ConversionType = conversionType.Value,
                CampaignId = leadState.CampaignId.Value,
                OccurredAt = payload.ReceivedAt,
                Amount = payload.Payout,
                Currency = payload.Currency,
            }, ct);
        }

        // RecalculateDeposits обновляет DepositCount в базе, но не сам объект leadState —
        // используем возвращённое значение ниже вместо устаревшего leadState.DepositCount,
        // иначе на RESALE в campaigns улетал бы счётчик на шаг позади (не считая текущий депозит).
        int? recalculatedDepositCount = null;

        var leadEventDocument = BuildLeadEventDocument(postbackEventType, payload, leadState.SpaceId);
        if (leadEventDocument is not null)
        {
            await leadEventsRepository.Create(leadEventDocument, ct);

            var isDeposit = postbackEventType is PostbackEventType.SALE or PostbackEventType.RESALE;
            if (isDeposit)
                recalculatedDepositCount = await leadStateRepository.RecalculateDeposits(payload.TenantId, payload.LeadId, ct);
        }

        // Не-ACCEPTED постбэк: событие записано, но лида по воронке не двигаем.
        // DUPLICATE — повторная доставка уже обработанного события, REPEAT — повторное событие
        // лида (например, вторая регистрация): переход по AutoAction уже был выполнен первым.
        if (payload.Status != PostbackEventStatus.ACCEPTED)
        {
            logger.LogInformation(
                "Postback {EventType} (eventId {EventId}, status {Status}) for lead '{LeadId}' — skipping funnel transition",
                postbackEventType, payload.EventId, payload.Status, payload.LeadId);
            return;
        }

        if (leadState.CampaignId is null)
        {
            logger.LogInformation(
                "Lead '{LeadId}' has no campaign, skipping postback {EventType}",
                payload.LeadId, postbackEventType);
            return;
        }

        var entryPoint = await campaignClient.GetAutoActionEntryPoint(
            payload.TenantId,
            leadState.CampaignId.Value,
            postbackEventType,
            recalculatedDepositCount ?? leadState.DepositCount,
            ct);

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

    private static LeadEventBaseDocument? BuildLeadEventDocument(PostbackEventType type, PostbackEventPayload payload, Guid spaceId)
    {
        // Статус переносим из постбэка как есть: дубли и повторы остаются в логе событий,
        // но в учёте депозитов не участвуют — пересчёт берёт только Accepted.
        var status = payload.Status switch
        {
            PostbackEventStatus.DUPLICATE => LeadEventStatus.Duplicate,
            PostbackEventStatus.REPEAT => LeadEventStatus.Repeat,
            _ => LeadEventStatus.Accepted
        };

        return type switch
        {
            PostbackEventType.REGISTRATION => new RegistrationLeadEvent
            {
                Id = Guid.CreateVersion7(),
                TenantId = payload.TenantId,
                SpaceId = spaceId,
                LeadId = payload.LeadId,
                EventId = payload.EventId,
                Status = status,
                CreatedAt = payload.ReceivedAt,
                CurrencyCode = payload.Currency ?? string.Empty
            },

            PostbackEventType.SALE => new SaleLeadEvent
            {
                Id = Guid.CreateVersion7(),
                TenantId = payload.TenantId,
                SpaceId = spaceId,
                LeadId = payload.LeadId,
                EventId = payload.EventId,
                Status = status,
                CreatedAt = payload.ReceivedAt,
                DepositAmount = payload.Payout ?? 0m,
                CurrencyCode = payload.Currency ?? string.Empty
            },

            PostbackEventType.RESALE => new ResaleLeadEvent
            {
                Id = Guid.CreateVersion7(),
                TenantId = payload.TenantId,
                SpaceId = spaceId,
                LeadId = payload.LeadId,
                EventId = payload.EventId,
                Status = status,
                CreatedAt = payload.ReceivedAt,
                DepositAmount = payload.Payout ?? 0m,
                CurrencyCode = payload.Currency ?? string.Empty
            },

            _ => null
        };
    }
}
