using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Dtos.Funnels;
using states.Services.Events.Producer.Payloads.Conversions;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Documents.Outbox;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(
    typeof(LeadStateCreatedOutboxDocument),
    typeof(LeadStatusChangedOutboxDocument),
    typeof(LeadFunnelPositionChangedOutboxDocument),
    typeof(LeadTagChangedOutboxDocument),
    typeof(LeadTranslatorChangedOutboxDocument),
    typeof(LeadPostbackParametersChangedOutboxDocument),
    typeof(LeadDepositChangedOutboxDocument),
    typeof(LeadConversionOutboxDocument))]
public abstract class OutboxDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    // null = unclaimed; set = claimed; set > timeout ago = re-claimable
    [BsonElement("claimedAt")]
    public DateTime? ClaimedAt { get; set; }

    [BsonElement("tenantId")]
    public Guid TenantId { get; set; }

    [BsonElement("spaceId")]
    public Guid SpaceId { get; set; }

    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    [BsonElement("leadId")]
    public string LeadId { get; set; } = default!;

    [BsonElement("version")]
    public long Version { get; set; }
}

public sealed class LeadStateCreatedOutboxDocument : OutboxDocument
{
    [BsonElement("campaignId")]
    public Guid? CampaignId { get; set; }

    [BsonElement("campaignName")]
    public string? CampaignName { get; set; }

    [BsonElement("sourceId")]
    public string? SourceId { get; set; }

    [BsonElement("sourceName")]
    public string? SourceName { get; set; }

    [BsonElement("funnelId")]
    public Guid? FunnelId { get; set; }

    [BsonElement("funnelName")]
    public string? FunnelName { get; set; }

    [BsonElement("flowId")]
    public Guid? FlowId { get; set; }

    [BsonElement("flowName")]
    public string? FlowName { get; set; }

    [BsonElement("nodeId")]
    public Guid? NodeId { get; set; }

    [BsonElement("nodeLabel")]
    public string NodeLabel { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LeadFunnelStatus Status { get; set; }

    [BsonElement("isInputTranslatorOn")]
    public bool IsInputTranslatorOn { get; set; }

    [BsonElement("isOutputTranslatorOn")]
    public bool IsOutputTranslatorOn { get; set; }

    [BsonElement("photoRecognition")]
    [BsonRepresentation(BsonType.String)]
    public RecognitionType PhotoRecognition { get; set; } = RecognitionType.Skip;

    [BsonElement("videoRecognition")]
    [BsonRepresentation(BsonType.String)]
    public RecognitionType VideoRecognition { get; set; } = RecognitionType.Skip;

    [BsonElement("voiceRecognition")]
    [BsonRepresentation(BsonType.String)]
    public RecognitionType VoiceRecognition { get; set; } = RecognitionType.Skip;

    // Начальные postback-параметры лида (обычно custom fields миграции) — едут внутри
    // created-события, а не отдельным LeadPostbackParametersChanged (см. CreateLeadState).
    [BsonElement("postbackParameters")]
    public Dictionary<string, string> PostbackParameters { get; set; } = [];

    // Начальные теги (перенесены миграцией) — тоже внутри created-события, а не отдельным
    // LeadTagChanged следом: на массовой материализации второе событие на каждого
    // тегированного лида удваивало бы волну outbox → Kafka → tgengine.
    [BsonElement("tags")]
    public List<TagDocument> Tags { get; set; } = [];
}

public sealed class LeadStatusChangedOutboxDocument : OutboxDocument
{
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LeadFunnelStatus Status { get; set; }
}

public sealed class LeadFunnelPositionChangedOutboxDocument : OutboxDocument
{
    [BsonElement("funnelId")]
    public Guid FunnelId { get; set; }

    [BsonElement("funnelName")]
    public string FunnelName { get; set; }

    [BsonElement("flowId")]
    public Guid FlowId { get; set; }

    [BsonElement("flowName")]
    public string FlowName { get; set; }

    [BsonElement("nodeId")]
    public Guid NodeId { get; set; }

    [BsonElement("nodeLabel")]
    public string NodeLabel { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LeadFunnelStatus Status { get; set; }
}

public sealed class LeadTagChangedOutboxDocument : OutboxDocument
{
    //[BsonElement("funnelId")]
    //public Guid FunnelId { get; set; }

    [BsonElement("tags")]
    public List<TagDocument> Tags { get; set; } = [];
    [BsonRepresentation(BsonType.String)]
    public TagOperation Operation { get; set; }
    public TagDocument? Tag { get; set; }
}

public sealed class LeadTranslatorChangedOutboxDocument : OutboxDocument
{
    [BsonElement("isInputTranslatorOn")]
    public bool IsInputTranslatorOn { get; set; }

    [BsonElement("isOutputTranslatorOn")]
    public bool IsOutputTranslatorOn { get; set; }
}

public sealed class LeadPostbackParametersChangedOutboxDocument : OutboxDocument
{
    [BsonElement("postbackParameters")]
    public Dictionary<string, string> PostbackParameters { get; set; } = [];
}

public sealed class LeadDepositChangedOutboxDocument : OutboxDocument
{
    [BsonElement("totalDepositAmount")]
    public decimal TotalDepositAmount { get; set; }

    [BsonElement("firstDepositAmount")]
    public decimal FirstDepositAmount { get; set; }

    [BsonElement("lastDepositAmount")]
    public decimal LastDepositAmount { get; set; }

    [BsonElement("depositCount")]
    public int DepositCount { get; set; }

    [BsonElement("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>
/// Конверсия лида для ФБ-пайплайна (топик lead-conversion-events, консюмер — campaigns).
/// Id документа = детерминированный id конверсии (EventId постбэка в трекере / Id лид-стейта
/// для контакта) — повторная постановка при переигрывании обработчика отбивается по _id.
/// Version для конверсий смысла не имеет, остаётся 0.
/// </summary>
public sealed class LeadConversionOutboxDocument : OutboxDocument
{
    [BsonElement("conversionType")]
    [BsonRepresentation(BsonType.String)]
    public LeadConversionType ConversionType { get; set; }

    [BsonElement("campaignId")]
    public Guid CampaignId { get; set; }

    // Момент конверсии (ReceivedAt постбэка / FirstContactAt), а не постановки в очередь
    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [BsonElement("amount")]
    public decimal? Amount { get; set; }

    [BsonElement("currency")]
    public string? Currency { get; set; }
}
