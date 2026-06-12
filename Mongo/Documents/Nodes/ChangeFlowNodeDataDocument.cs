using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.ChangeFlow))]
    public sealed record ChangeFlowNodeDataDocument : NodeDataDocument
    {
        [BsonElement("flowId")]
        public Guid FlowId { get; init; }
        [BsonElement("nodeId")]
        public Guid NodeId { get; init; }

    }
}
