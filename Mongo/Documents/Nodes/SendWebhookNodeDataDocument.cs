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
        public WebhookMethodType MethodType { get; set; }

    }
}
