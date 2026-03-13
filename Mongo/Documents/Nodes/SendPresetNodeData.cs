using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.SendPreset))]
    public sealed record SendPresetNodeData : NodeData
    {
        [BsonElement("actions")]
        public List<SendPresetAction> Actions { get; init; } = [];
    }
}
