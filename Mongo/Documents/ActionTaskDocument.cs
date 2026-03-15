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
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonElement("leadStateId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid LeadStateId { get; set; }

    [BsonElement("funnelId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid FunnelId { get; set; }

    [BsonElement("flowId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid FlowId { get; set; }

    [BsonElement("nodeId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid NodeId { get; set; }

    [BsonElement("actionId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
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

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    protected ActionTaskDocument(ActionType type)
    {
        Type = type;
        Status = ActionStatus.Pending;
    }
}

public sealed class SendPresetActionTaskDocument : ActionTaskDocument
{
    [BsonElement("presetId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid PresetId { get; set; }

    [BsonElement("needPin")]
    public bool NeedPin { get; set; }

    public SendPresetActionTaskDocument() : base(ActionType.SendPreset) { }
}

public sealed class ManageTagActionTaskDocument : ActionTaskDocument
{
    [BsonElement("operation")]
    [BsonRepresentation(BsonType.String)]
    public TagOperation Operation { get; set; }

    [BsonElement("tagId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid TagId { get; set; }

    [BsonElement("replacementTagId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? ReplacementTagId { get; set; }

    public ManageTagActionTaskDocument() : base(ActionType.ManageTag) { }
}
