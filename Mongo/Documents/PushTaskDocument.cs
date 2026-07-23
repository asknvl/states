using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents;

public sealed class PushTaskDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("tenantId")]
    public Guid TenantId { get; set; }

    [BsonElement("spaceId")]
    public Guid SpaceId { get; set; }

    [BsonElement("leadStateId")]
    public Guid LeadStateId { get; set; }

    [BsonElement("funnelId")]
    public Guid FunnelId { get; set; }

    [BsonElement("flowId")]
    public Guid FlowId { get; set; }

    [BsonElement("nodeId")]
    public Guid NodeId { get; set; }

    [BsonElement("pushId")]
    public Guid PushId { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ActionStatus Status { get; set; } = ActionStatus.Pending;

    [BsonElement("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("claimedAt")]
    public DateTime? ClaimedAt { get; set; }

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    [BsonElement("presetId")]
    public Guid PresetId { get; set; }

    // Собственный delay пуша из конфига ноды (не накопленный). Нужен воркеру, чтобы пересчитать
    // ScheduledAt без похода в конфиг воронки: якорь таска = ScheduledAt - Delay.
    [BsonElement("delay")]
    public TimeSpan? Delay { get; set; }

    // Пуш AiReply-ноды — «таймер неактивности»: отправляется только если лид молчит. Для таких
    // тасков воркер перед отправкой сверяет якорь с FunnelLeadState.LastIncomingAt и при
    // необходимости откладывает отправку (см. PushWorkerService), а UnlockNext перезаякоривает
    // следующий пуш цепочки от момента фактической отправки предыдущего.
    [BsonElement("isInactivityPush")]
    public bool IsInactivityPush { get; set; }
}
