using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(SendPresetActionTaskDocument))]
[BsonKnownTypes(typeof(ManageTagActionTaskDocument))]
public abstract class ActionTaskDocument
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

    [BsonElement("actionId")]
    public Guid ActionId { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ActionType Type { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ActionStatus Status { get; set; }

    [BsonElement("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("claimedAt")]
    public DateTime? ClaimedAt { get; set; }

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("isCritical")]
    public bool IsCritical { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    protected ActionTaskDocument(ActionType type, bool isCritical)
    {
        Type = type;
        IsCritical = isCritical;
        Status = ActionStatus.Pending;
    }
}

public sealed class SendPresetActionTaskDocument : ActionTaskDocument
{
    [BsonElement("botId")]
    public Guid BotId { get; set; }

    [BsonElement("chatId")]
    public Guid ChatId { get; set; }

    [BsonElement("presetId")]
    public Guid PresetId { get; set; }

    [BsonElement("needPin")]
    public bool NeedPin { get; set; }

    public SendPresetActionTaskDocument() : base(ActionType.SendPreset, isCritical: true) { }
}

public sealed class ManageTagActionTaskDocument : ActionTaskDocument
{
    [BsonElement("operation")]
    [BsonRepresentation(BsonType.String)]
    public TagOperation Operation { get; set; }

    [BsonElement("tagId")]
    public Guid TagId { get; set; }    

    [BsonElement("replacementTagId")]
    public Guid? ReplacementTagId { get; set; }    
    public ManageTagActionTaskDocument() : base(ActionType.ManageTag, isCritical: true) { }
}
