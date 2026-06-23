using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.SendWebhook))]
    public sealed record SendWebhookNodeDataDocument : NodeDataDocument
    {
        [BsonElement("url")]
        public string Url { get; set; }

        [BsonElement("methodType")]
        [BsonRepresentation(BsonType.String)]
        public WebhookMethodType MethodType { get; set; }

    }
}
