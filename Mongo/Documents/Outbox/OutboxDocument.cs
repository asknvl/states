using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Dtos.Funnels;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Documents.Outbox;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(
    typeof(LeadStateCreatedOutboxDocument),
    typeof(LeadStatusChangedOutboxDocument),
    typeof(LeadFunnelPositionChangedOutboxDocument),
    typeof(LeadTagChangedOutboxDocument))]
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
    public Guid CampaignId { get; set; }

    [BsonElement("campaignName")]
    public string CampaignName { get; set; }

    [BsonElement("sourceId")]
    public string SourceId { get; set; }

    [BsonElement("sourceName")]
    public string SourceName { get; set; }

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
    [BsonElement("funnelId")]
    public Guid FunnelId { get; set; }

    [BsonElement("tags")]
    public List<TagDocument> Tags { get; set; } = [];
    [BsonRepresentation(BsonType.String)]
    public TagOperation Operation { get; set; }
    public TagDocument Tag { get; set; }

}
