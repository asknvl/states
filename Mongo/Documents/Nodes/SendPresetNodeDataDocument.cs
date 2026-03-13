using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Actions;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.SendPreset))]
    public sealed record SendPresetNodeDataDocument : NodeDataDocument
    {
        [BsonElement("actions")]
        public List<SendPresetActionDocument> Actions { get; init; } = [];
    }
}
