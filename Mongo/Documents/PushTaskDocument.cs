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
}
